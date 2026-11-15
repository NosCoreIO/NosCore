//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;
using NosCore.Packets.ServerPackets.Player;

namespace NosCore.GameObject.Ecs.Systems;

public class AmbientEffectSystem
{
    private readonly QueryDescription _effectQuery = new QueryDescription()
        .WithAll<EntityIdentityComponent, EffectComponent, HealthComponent>();

    public void TakeDue(MapWorld world, Instant now, List<EffectPacket> due)
    {
        world.Exclusive(w => w.Query(in _effectQuery, (Entity entity, ref EntityIdentityComponent identity, ref EffectComponent effect, ref HealthComponent health) =>
        {
            if (effect.Effect <= 0 || !health.IsAlive
                || now - effect.LastPlayed < Duration.FromMilliseconds(effect.EffectDelay))
            {
                return;
            }

            effect = effect with { LastPlayed = now };
            due.Add(new EffectPacket { EffectType = identity.VisualType, VisualEntityId = identity.VisualId, Id = effect.Effect });
        }));
    }
}

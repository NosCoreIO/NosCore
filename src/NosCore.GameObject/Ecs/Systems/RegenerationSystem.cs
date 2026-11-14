//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;

namespace NosCore.GameObject.Ecs.Systems;

// Standing regeneration waits for 4 seconds without damage; without it the bar refills mid-fight.
public class RegenerationSystem
{
    private static readonly Duration SittingInterval = Duration.FromMilliseconds(1500);
    private static readonly Duration StandingInterval = Duration.FromMilliseconds(2000);
    private static readonly Duration StandingDefenceGrace = Duration.FromSeconds(4);

    private readonly QueryDescription _playerQuery = new QueryDescription()
        .WithAll<PlayerComponent, HealthComponent, ManaComponent, VisualComponent, TimingComponent>();

    // Lists the players whose regeneration step has come and starts their next interval.
    public void FindDue(MapWorld world, Instant now, List<Entity> due)
    {
        world.Exclusive(w => w.Query(in _playerQuery, (Entity entity, ref HealthComponent health, ref ManaComponent mana,
            ref VisualComponent visual, ref TimingComponent timing) =>
        {
            if (!health.IsAlive || (health.Hp >= health.MaxHp && mana.Mp >= mana.MaxMp))
            {
                return;
            }

            if (timing.LastRegen == Instant.MinValue)
            {
                timing = timing with { LastRegen = now };
            }

            if (now - timing.LastRegen < (visual.IsSitting ? SittingInterval : StandingInterval))
            {
                return;
            }

            timing = timing with { LastRegen = now };
            if (!visual.IsSitting && now - timing.LastDamaged < StandingDefenceGrace)
            {
                return;
            }

            due.Add(entity);
        }));
    }
}

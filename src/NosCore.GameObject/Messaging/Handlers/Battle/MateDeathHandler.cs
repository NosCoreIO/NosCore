//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MateService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class MateDeathHandler(ISessionRegistry sessionRegistry, IMateRevivalService mateRevivalService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not MateComponentBundle mate
                || !sessionRegistry.TryGetCharacter(s => s.CharacterId == mate.OwnerId, out var owner))
            {
                return Task.CompletedTask;
            }

            return mateRevivalService.KnockOutAsync(owner, mate.Mate);
        }
    }
}

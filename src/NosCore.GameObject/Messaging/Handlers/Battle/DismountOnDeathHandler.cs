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
using NosCore.GameObject.Services.TransformationService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class DismountOnDeathHandler(ISessionRegistry sessionRegistry, ITransformationService transformationService)
    {
        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not PlayerComponentBundle { IsVehicled: true } player
                || sessionRegistry.GetSessionByCharacterId(player.CharacterId) is not { } session)
            {
                return;
            }

            await transformationService.RemoveVehicleAsync(session);
        }
    }
}

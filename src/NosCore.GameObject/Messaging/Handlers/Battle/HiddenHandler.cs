//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // A hidden player stays on the map but the other clients stop drawing them; their own client draws
    // them see-through.
    [UsedImplicitly]
    public sealed class HiddenHandler(IBuffService buffService)
    {
        [UsedImplicitly]
        public Task Handle(HiddenChangedEvent evt)
        {
            if (evt.Target is not PlayerComponentBundle player || player.MapInstance == null)
            {
                return Task.CompletedTask;
            }

            var hidden = buffService.HasEffect(player, BCardEffect.SpecialActionsHide);
            if (player.Invisible == hidden)
            {
                return Task.CompletedTask;
            }

            player.Camouflage = hidden;
            player.Invisible = hidden;
            return player.MapInstance.SendPacketAsync(player.GenerateInvisible());
        }
    }
}

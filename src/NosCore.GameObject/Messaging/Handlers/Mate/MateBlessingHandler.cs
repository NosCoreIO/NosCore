//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MateService;

namespace NosCore.GameObject.Messaging.Handlers.Mate
{
    [UsedImplicitly]
    public sealed class MateBlessingHandler(IMateBlessingService mateBlessingService)
    {
        [UsedImplicitly]
        public Task Handle(BuffExpiredEvent evt) => mateBlessingService.RenewAsync(evt.Character, evt.CardId);

        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt) =>
            evt.PlayerHasLeft ? Task.CompletedTask : mateBlessingService.BlessAsync(evt.ClientSession.Character);
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class LowLevelBuffHandler(ILowLevelBuffService lowLevelBuffService)
    {
        [UsedImplicitly]
        public Task Handle(BuffExpiredEvent evt) => lowLevelBuffService.RenewAsync(evt.Character, evt.CardId);

        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt) =>
            evt.PlayerHasLeft ? Task.CompletedTask : lowLevelBuffService.RefreshAsync(evt.ClientSession.Character);
    }
}

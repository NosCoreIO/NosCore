//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.Map
{
    [UsedImplicitly]
    public sealed class HeatEntranceHandler(IHeatService heatService)
    {
        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt) => heatService.RefreshAsync(evt.ClientSession.Character);
    }
}

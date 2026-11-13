//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.LandOfDeathService;

namespace NosCore.GameObject.Messaging.Handlers.Map
{
    [UsedImplicitly]
    public sealed class LandOfDeathExitHandler(ILandOfDeathService landOfDeathService)
    {
        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt) =>
            evt.LeftMapInstance.MapInstanceType == MapInstanceType.LodInstance
            && evt.MapInstance.MapInstanceType != MapInstanceType.LodInstance
                ? landOfDeathService.LeaveAsync(evt.ClientSession)
                : Task.CompletedTask;
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Algorithm.DignityService;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;

namespace NosCore.GameObject.Messaging.Handlers.TimeSpace
{
    [UsedImplicitly]
    public sealed class TimeSpaceTimedOutHandler(IDignityService dignityService)
    {
        [UsedImplicitly]
        public Task Handle(ScriptedInstanceTimedOutEvent evt) =>
            evt.Character.LoseDignityAsync(dignityService, dignityService.GetDignityLoss(evt.Character.Level));
    }
}

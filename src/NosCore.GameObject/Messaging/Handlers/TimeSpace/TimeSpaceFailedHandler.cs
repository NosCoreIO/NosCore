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
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.TimeSpace
{
    // A failed run costs each player inside their level in dignity, whether the clock or the lives ran out.
    [UsedImplicitly]
    public sealed class TimeSpaceFailedHandler(IDignityService dignityService)
    {
        [UsedImplicitly]
        public async Task Handle(TimeSpaceFailedEvent evt)
        {
            await evt.Character.LoseDignityAsync(dignityService, dignityService.GetDignityLoss(evt.Character.Level));
            var failures = evt.Character.RecordTimeSpaceFailure(evt.TimeSpaceId);
            if (failures >= TimeSpaceAttemptExtensions.LoserModeFailures)
            {
                await evt.Character.SendPacketAsync(evt.Character.GenerateSayi(SayColorType.Red,
                    Game18NConstString.LoserModeAvailable, 4, failures));
            }
        }
    }
}

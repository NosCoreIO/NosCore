//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.TimeSpace
{
    [UsedImplicitly]
    public sealed class TimeSpaceFailedHandler(ITimeSpaceAttemptService timeSpaceAttemptService)
    {
        [UsedImplicitly]
        public async Task Handle(TimeSpaceFailedEvent evt)
        {
            if (await timeSpaceAttemptService.RecordFailureAsync(evt.Character.CharacterId, evt.TimeSpaceId) is { } failures
                && failures >= TimeSpaceAttemptService.LoserModeFailures)
            {
                await evt.Character.SendPacketAsync(evt.Character.GenerateSayi(SayColorType.Red,
                    Game18NConstString.LoserModeAvailable, 4, failures));
            }
        }
    }
}

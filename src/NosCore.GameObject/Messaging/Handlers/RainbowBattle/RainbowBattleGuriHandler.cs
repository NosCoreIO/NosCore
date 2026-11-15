//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.RainbowBattleService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.RainbowBattle
{
    // The sign-up answer, and the capture and rescue bars coming back once full.
    [UsedImplicitly]
    public sealed class RainbowBattleGuriHandler(IRainbowBattleService rainbowBattleService)
    {
        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            var session = evt.ClientSession;
            return evt.Packet switch
            {
                { Type: GuriPacketType.RainbowBattleRegistration } => rainbowBattleService.RegisterAsync(session),
                { Type: GuriPacketType.RainbowBattleCapture, Argument: { } crystalId } when session.HasPlayerEntity =>
                    rainbowBattleService.CompleteCaptureAsync(session.Character, crystalId),
                { Type: GuriPacketType.RainbowBattleUnfreeze, VisualId: { } frozenId } when session.HasPlayerEntity =>
                    rainbowBattleService.CompleteRescueAsync(session.Character, frozenId),
                _ => Task.CompletedTask,
            };
        }
    }
}

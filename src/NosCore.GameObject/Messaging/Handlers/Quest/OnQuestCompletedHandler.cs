//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quest;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    [UsedImplicitly]
    public sealed class OnQuestCompletedHandler(IQuestService questService, IClock clock)
    {
        [UsedImplicitly]
        public async Task Handle(QuestCompletedEvent evt)
        {
            await evt.Character.SendPacketAsync(evt.Character.GenerateCompletedQuests(SqstPacket.BlockOf(evt.Quest.QuestId), clock.GetCurrentInstant()));
            await evt.Character.SendPacketAsync(evt.Character.GenerateQuestPacket());
            if (evt.Quest.Quest.NextQuestId is { } next)
            {
                await questService.AddQuestAsync(evt.Character, QuestActionType.Achieve, next);
            }
        }
    }
}

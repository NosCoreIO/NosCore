//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 200 {questId}: the NPC dialog button that hands a held Dialog2 quest in.
    [UsedImplicitly]
    public sealed class QuestDialogHandler(
        IQuestService questService,
        IEnumerable<IQuestTypeHandler> questTypeHandlers) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GetXQuestFromDialog2Quest;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not INonPlayableEntity { NpcMonster: { } npc } || packet.Type is not { } questId)
            {
                return;
            }

            var character = session.Character;
            var quest = character.Quests.Values.FirstOrDefault(q =>
                q.QuestId == questId && q.ObjectivesCompletedOn is null && q.CompletedOn is null);
            var handler = quest == null ? null : questTypeHandlers.FirstOrDefault(h => h.QuestType == quest.Quest.QuestType);
            if (handler != null && await handler.OnQuestDialogAnsweredAsync(character, npc, quest!))
            {
                await questService.CompleteQuestAsync(character, quest!);
            }
        }
    }
}

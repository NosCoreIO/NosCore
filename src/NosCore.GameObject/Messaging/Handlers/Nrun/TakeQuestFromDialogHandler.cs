//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 3006 {questId}: the NPC dialog button that hands the player a quest.
    [UsedImplicitly]
    public sealed class TakeQuestFromDialogHandler(IQuestService questService, List<QuestDto> quests) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GetXQuest;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (packet.Type is not { } questId || quests.FirstOrDefault(q => q.QuestId == questId) is not { } quest)
            {
                return;
            }

            var character = session.Character;
            if (quest.RequiredQuestId is { } required
                && !character.Quests.Values.Any(q => q.QuestId == required && q.CompletedOn != null))
            {
                return;
            }

            await questService.AddQuestAsync(character, QuestActionType.Achieve, questId);
        }
    }
}

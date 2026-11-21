//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // A dialog button whose argument is the id of the quest it hands out.
    public abstract class NamedQuestDialogHandler(IQuestService questService, IQuestCatalog quests,
        INpcTalkButtonCatalog npcTalkButtons) : INrunEventHandler
    {
        public abstract NrunRunnerType Runner { get; }

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (packet.Type is not { } questId || quests.GetQuest(questId) is not { } quest
                || target is not NpcComponentBundle npc || !npcTalkButtons.Offers(npc, Runner, questId))
            {
                return;
            }

            if (!session.Character.HasDoneRequiredQuest(quest))
            {
                return;
            }

            await questService.AddQuestAsync(session.Character, QuestActionType.Achieve, questId);
        }
    }

    // n_run 3006 {questId}: the NPC dialog button that hands the player a quest.
    [UsedImplicitly]
    public sealed class TakeQuestFromDialogHandler(IQuestService questService, IQuestCatalog quests,
            INpcTalkButtonCatalog npcTalkButtons)
        : NamedQuestDialogHandler(questService, quests, npcTalkButtons)
    {
        public override NrunRunnerType Runner => NrunRunnerType.GetXQuest;
    }

    // n_run 2000 {questId}: the NPC dialog button that starts a specialist card or seal quest.
    [UsedImplicitly]
    public sealed class TakeSpecialistQuestFromDialogHandler(IQuestService questService, IQuestCatalog quests,
            INpcTalkButtonCatalog npcTalkButtons)
        : NamedQuestDialogHandler(questService, quests, npcTalkButtons)
    {
        public override NrunRunnerType Runner => NrunRunnerType.GetXSPQuest;
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class MerlingsCurseQuestHandler(IQuestService questService, IQuestCatalog quests,
            INpcTalkButtonCatalog npcTalkButtons)
        : NamedQuestDialogHandler(questService, quests, npcTalkButtons)
    {
        public override NrunRunnerType Runner => NrunRunnerType.GetQuestTheMerlingsCurse;
    }
}

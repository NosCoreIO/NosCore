//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class FreshLiverQuestHandler(IQuestService questService, List<QuestDto> quests, IRandomProvider random, IClock clock,
        INpcTalkButtonCatalog npcTalkButtons)
        : DialogQuestOfferHandler(questService, quests, random, clock, npcTalkButtons)
    {
        public override NrunRunnerType Runner => NrunRunnerType.GetQuestFreshLiverPlease;
    }
}

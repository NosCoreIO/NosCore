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
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class BurningSwordMonstersDailyQuestHandler(IQuestService questService, List<QuestDto> quests, IRandomProvider random, IClock clock)
        : DialogQuestOfferHandler(questService, quests, random, clock)
    {
        public override NrunRunnerType Runner => NrunRunnerType.GetDailyQuestAkamurMerchantMilitaryEngineerEliminateMonsters;
    }
}

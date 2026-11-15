//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using System.Linq;
using NosCore.Packets.Enumerations;

namespace NosCore.Parser.Parsers
{
    // The quests an NPC dialog button hands out without naming one: the button offers one of its
    // quests at random. Matched by the quest's title, since neither quest.dat nor npctalk.dat links them.
    public static class DialogQuestOffers
    {
        private static readonly Dictionary<NrunRunnerType, short[]> Offers = new()
        {
            [NrunRunnerType.GetDailyQuestSerizardIceFlowerOil] = [5514],
            [NrunRunnerType.GetDailyQuestFireAlchemistHeatResistancePotion] = [5517, 5914],
            [NrunRunnerType.GetDailyQuestAkamurMerchantMilitaryEngineerEliminateMonsters] = [5515, 5905, 5906, 5908],
            [NrunRunnerType.GetDailyQuestContructionMaterial] =
                [5519, 5910, 5911, 5912, 5916, 5917, 5918, 5919, 5920, 5921],
            [NrunRunnerType.GetDailyQuestEasterMalcolmMix] = [5938, 5945],
            [NrunRunnerType.GetQuestFromNPC] = [2251, 2252, 2253, 2254, 2255],
            [NrunRunnerType.OldGetQuestEasterCalvinCoach] = [2250],
        };

        public static NrunRunnerType? OfferedBy(short questId) =>
            Offers.Where(o => o.Value.Contains(questId)).Select(o => (NrunRunnerType?)o.Key).FirstOrDefault();
    }
}

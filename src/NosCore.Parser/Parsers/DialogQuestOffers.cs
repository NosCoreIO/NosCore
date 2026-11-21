//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Packets.Enumerations;

namespace NosCore.Parser.Parsers
{
    // The quests an NPC dialog button hands out without naming one: the button offers one of its
    // quests at random. Matched by the quest's title, objective NPC and reward against the dialog,
    // since neither quest.dat nor npctalk.dat links them.
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
            [NrunRunnerType.GetQuestChristmasTheVikingFamily] = [5970],
            [NrunRunnerType.GetQuestGingsengDagger] = [6036],
            [NrunRunnerType.GetQuestGrahamCrusarder] = [6040],
            [NrunRunnerType.GetQuestChristmasDestroyFiends] = [6325],
            [NrunRunnerType.GetQuestAmoraStory] = [6326],
            [NrunRunnerType.GetDailyQuestEasterEvaEnergy2] = [7550],
            [NrunRunnerType.GetDailyQuestChristmasEvaEnergyHugeSnowman] = [5936],
            [NrunRunnerType.GetQuestFreshLiverPlease] = [6002],
            [NrunRunnerType.GetQuestFoxysVerdict] = [6003],
            [NrunRunnerType.GetQuestFoxHunt] = [6004],
            [NrunRunnerType.GetQuestDefeatLordMelonoth] = [7163],
            [NrunRunnerType.GetDailyQuestReturnOfTheMelonLord] = [7724],
            [NrunRunnerType.GetDailyQuestLabOnAlert] = [7725],
            [NrunRunnerType.GetQuestThePathToTheLostCatacombs] = [7547],
            [NrunRunnerType.GetQuestEasterNotEnoughIngredients] = [7551],
            [NrunRunnerType.GetQuestCauseForTheArrival] = [6500],
            // The only Valentine's quest line, and the one that reports back to Amora.
            [NrunRunnerType.GetQuestAmoraValentinesDay] = [6360],
            [NrunRunnerType.GetQuestTheCursed13] = [6369],
        };

        // Buttons whose argument picks the quest: the quest at that index. The score quest buttons
        // are labelled with the level range of the quest they stand for.
        private static readonly Dictionary<NrunRunnerType, short[]> Choices = new()
        {
            [NrunRunnerType.GetQuestHigherTimeSpaceScore] =
                [900, 901, 902, 903, 904, 905, 906, 907, 908, 909, 910, 911, 912, 913],
        };

        public static NrunRunnerType? OfferedBy(short questId) =>
            Offers.Concat(Choices).Where(o => o.Value.Contains(questId)).Select(o => (NrunRunnerType?)o.Key).FirstOrDefault();

        public static short? OfferOption(short questId) =>
            Choices.Values.Where(c => c.Contains(questId)).Select(c => (short?)Array.IndexOf(c, questId)).FirstOrDefault();
    }
}

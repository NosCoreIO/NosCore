//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers

{
    //#=======
    //BEGIN
    //VNUM    {id}    {rewardType}
    //DATA    {c1}    {c2}    {c3}    {c4}    {c5}
    //END

    public class QuestPrizeParser(IDao<QuestRewardDto, short> questRewardDtoDao, ILoggerFactory loggerFactory, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly ILogger<QuestPrizeParser> _logger = loggerFactory.CreateLogger<QuestPrizeParser>();
        private readonly string _fileQuestPrizeDat = $"{Path.DirectorySeparatorChar}qstprize.dat";

        public async Task ImportQuestPrizesAsync(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(QuestRewardDto.QuestRewardId), chunk => Convert.ToInt16(chunk["VNUM"][0][1])},
                {nameof(QuestRewardDto.RewardType), chunk => RewardType(chunk)},
                {nameof(QuestRewardDto.Data), chunk => ImportData(chunk)},
                {nameof(QuestRewardDto.SecondData), chunk => ImportSecondData(chunk)},
                {nameof(QuestRewardDto.ThirdData), chunk => IsItemChoice(RewardType(chunk)) ? Column(chunk, 3) : null},
                {nameof(QuestRewardDto.FourthData), chunk => IsItemChoice(RewardType(chunk)) ? Column(chunk, 4) : null},
                {nameof(QuestRewardDto.Amount), chunk => ImportAmount(chunk)},
            };
            var genericParser = new GenericParser<QuestRewardDto>(folder + _fileQuestPrizeDat, "END", 0, actionList,
                loggerFactory.CreateLogger<GenericParser<QuestRewardDto>>(), logLanguage);
            var questRewardDtos = await genericParser.GetDtosAsync();
            await questRewardDtoDao.TryInsertOrUpdateAsync(questRewardDtos);
            _logger.LogInformation(logLanguage[LogLanguageKey.QUEST_PRIZES_PARSED], questRewardDtos.Count);
        }

        private static QuestRewardType RewardType(Dictionary<string, string[][]> chunk) =>
            (QuestRewardType)Convert.ToInt16(chunk["VNUM"][0][2]);

        private static int? Column(Dictionary<string, string[][]> chunk, int column)
        {
            var value = int.Parse(chunk["DATA"][0][column]);
            return value == -1 ? null : value;
        }

        // EtcMainItem, WearItem and ClassItem list up to four item vnums in c1-c4.
        private static bool IsItemChoice(QuestRewardType type) =>
            type is QuestRewardType.EtcMainItem or QuestRewardType.WearItem or QuestRewardType.ClassItem;

        // Data is the item vnum, the target level, or for the experience kinds the level whose bar the percentage
        // applies to (negative: the character's own level).
        private static int ImportData(Dictionary<string, string[][]> chunk) => RewardType(chunk) switch
        {
            QuestRewardType.Experience or QuestRewardType.JobExperience or QuestRewardType.HeroExperience => Column(chunk, 2) ?? 0,
            QuestRewardType.ConsecutiveItem or QuestRewardType.Level or QuestRewardType.LevelAndJobLevel => Column(chunk, 1) ?? 0,
            var type when IsItemChoice(type) => Column(chunk, 1) ?? 0,
            _ => 0,
        };

        private static int? ImportSecondData(Dictionary<string, string[][]> chunk) => RewardType(chunk) switch
        {
            QuestRewardType.LevelAndJobLevel => Column(chunk, 2),
            var type when IsItemChoice(type) => Column(chunk, 2),
            _ => null,
        };

        // A percentage for the experience kinds, a count for items, gold or reputation otherwise.
        private static int ImportAmount(Dictionary<string, string[][]> chunk) => RewardType(chunk) switch
        {
            QuestRewardType.ConsecutiveItem => Column(chunk, 3) ?? 1,
            var type when IsItemChoice(type) => Column(chunk, 5) ?? 1,
            QuestRewardType.NotRepeatable or QuestRewardType.Level or QuestRewardType.LevelAndJobLevel => 0,
            _ => Column(chunk, 1) ?? 0,
        };
    }
}

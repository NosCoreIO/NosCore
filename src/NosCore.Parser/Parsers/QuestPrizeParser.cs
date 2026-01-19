//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Quest;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers

{
    //#=======
    //BEGIN
    //VNUM    1	2
    //DATA	10	-1	-1	-1	-1
    //END

    public class QuestPrizeParser(IDao<QuestRewardDto, short> questRewardDtoDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly string _fileQuestPrizeDat = $"{Path.DirectorySeparatorChar}qstprize.dat";


        public async Task ImportQuestPrizesAsync(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(QuestRewardDto.QuestRewardId), chunk => Convert.ToInt16(chunk["VNUM"][0][1])},
                {nameof(QuestRewardDto.RewardType), chunk => Convert.ToByte(chunk["VNUM"][0][2])},
                {nameof(QuestRewardDto.Data), chunk => ImportData(chunk)},
                {nameof(QuestRewardDto.Amount), chunk => ImportAmount(chunk)},
            };
            var genericParser = new GenericParser<QuestRewardDto>(folder + _fileQuestPrizeDat, "END", 0, actionList, logger, logLanguage);
            var questRewardDtos = await genericParser.GetDtosAsync();
            await questRewardDtoDao.TryInsertOrUpdateAsync(questRewardDtos);
            logger.Information(logLanguage[LogLanguageKey.QUEST_PRIZES_PARSED], questRewardDtos.Count);
        }

        private int ImportData(Dictionary<string, string[][]> chunk)
        {
            var rewardType = (QuestRewardType)Convert.ToInt64(chunk["VNUM"][0][2]);
            return rewardType switch
            {
                QuestRewardType.Exp => int.Parse(chunk["DATA"][0][2]) == -1 ? 0 : int.Parse(chunk["DATA"][0][2]),
                QuestRewardType.PercentExp => int.Parse(chunk["DATA"][0][2]) == -1 ? 0 : int.Parse(chunk["DATA"][0][2]),
                QuestRewardType.JobExp => int.Parse(chunk["DATA"][0][2]) == -1 ? 0 : int.Parse(chunk["DATA"][0][2]),
                QuestRewardType.PercentJobExp => int.Parse(chunk["DATA"][0][2]) == -1 ? 0 : int.Parse(chunk["DATA"][0][2]),
                QuestRewardType.WearItem => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.EtcMainItem => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.Gold => 0,
                QuestRewardType.BaseGoldByAmount => 0,
                QuestRewardType.CapturedGold => 0,
                QuestRewardType.UnknowGold => 0,
                QuestRewardType.Reput => 0,
                _ => int.Parse(chunk["DATA"][0][1]),
            };
        }

        private int ImportAmount(Dictionary<string, string[][]> chunk)
        {
            var rewardType = (QuestRewardType)Convert.ToInt64(chunk["VNUM"][0][2]);
            return rewardType switch
            {
                QuestRewardType.Exp => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.PercentExp => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.JobExp => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.PercentJobExp => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.WearItem => 1,
                QuestRewardType.EtcMainItem => int.Parse(chunk["DATA"][0][5]) == -1 ? 1 : int.Parse(chunk["DATA"][0][5]),
                QuestRewardType.Gold => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.BaseGoldByAmount => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.CapturedGold => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.UnknowGold => int.Parse(chunk["DATA"][0][1]),
                QuestRewardType.Reput => int.Parse(chunk["DATA"][0][1]),

                _ => int.Parse(chunk["DATA"][0][2]),
            };
        }
    }
}
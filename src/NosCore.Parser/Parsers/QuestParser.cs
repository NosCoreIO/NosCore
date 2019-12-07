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

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using Serilog;

namespace NosCore.Parser.Parsers

{

    //BEGIN
    //VNUM    {QuestId}     {QuestType}   {autoFinish}    {Daily}	{requiredQuest}    {Secondary}
    //LEVEL   {LevelMin}	{LevelMax}
    //TITLE   {Title}
    //DESC    {Desc}
    //TALK    {StartDialogId}	{EndDialogId}	0	0
    //TARGET  {TargetX}    {TargetY}	{TargetMap}
    //DATA	  0    1	-1     1
    //PRIZE	  {firstPrizeVNUM}	{secondPrizeVNUM}	{thirdPrizeVNUM}	{fourthPrizeVNUM}
    //LINK	  {NextQuest}
    //END
    //
    //#=======

    public class QuestParser
    {
        private readonly string _fileQuestDat = "\\quest.dat";
        private readonly ILogger _logger;
        private readonly IGenericDao<QuestDto> _questDao;
        private readonly IGenericDao<QuestObjectiveDto> _questObjectiveDao;


        public QuestParser(IGenericDao<QuestDto> questDao, IGenericDao<QuestObjectiveDto> questObjectiveDao, ILogger logger)
        {
            _logger = logger;
            _questDao = questDao;
            _questObjectiveDao = questObjectiveDao;
        }

        public void ImportQuests(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object>>
            {
                {nameof(QuestDto.QuestId), chunk => Convert.ToInt16(chunk["VNUM"][0][1])},
                {nameof(QuestDto.QuestType), chunk => Convert.ToInt32(chunk["VNUM"][0][2])},
                {nameof(QuestDto.AutoFinish), chunk => chunk["VNUM"][0][3] == "1"},
                {nameof(QuestDto.IsDaily), chunk => chunk["VNUM"][0][4] != "-1"},
                {nameof(QuestDto.IsSecondary), chunk => chunk["VNUM"][0][5] != "-1"},
                {nameof(QuestDto.LevelMin), chunk => Convert.ToByte(chunk["LEVEL"][0][1])},
                {nameof(QuestDto.LevelMax), chunk => Convert.ToByte(chunk["LEVEL"][0][2])},
                {nameof(QuestDto.Title), chunk => chunk["TITLE"][0][1]},
                {nameof(QuestDto.Desc), chunk => chunk["DESC"][0][1]},
                {nameof(QuestDto.TargetX), chunk =>  chunk["TARGET"][0][1] == "-1" ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][1])},
                {nameof(QuestDto.TargetY), chunk =>  chunk["TARGET"][0][2] == "-1"  ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][2])},
                {nameof(QuestDto.TargetMap), chunk => chunk["TARGET"][0][3] == "-1"  ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][3])},
                {nameof(QuestDto.StartDialogId), chunk => chunk["TARGET"][0][1] == "-1" ? (int?)null :  Convert.ToInt32(chunk["TALK"][0][1])},
                {nameof(QuestDto.EndDialogId), chunk => chunk["TARGET"][0][2] == "-1" ? (int?)null :  Convert.ToInt32(chunk["TALK"][0][2])},
            };
            var genericParser = new GenericParser<QuestDto>(folder + _fileQuestDat, "#=======", 0, actionList, _logger);
            var quests = genericParser.GetDtos("    ");

            _questDao.InsertOrUpdate(quests);
            _questObjectiveDao.InsertOrUpdate(quests.Where(s => s.QuestObjective != null).SelectMany(s => s.QuestObjective));

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.QUESTS_PARSED), quests.Count);
        }
    }

    //public void ImportQuests()
    //{
    //                switch (currentLine[0])
    //                {

    //                    case "LINK":
    //                        if (int.Parse(currentLine[1]) != -1) // Base Quest Order (ex: SpQuest)
    //                        {
    //                            quest.NextQuestId = int.Parse(currentLine[1]);
    //                            continue;
    //                        }

    //                        // Main Quest Order
    //                        switch (quest.QuestId)
    //                        {
    //                            case 1997:
    //                                quest.NextQuestId = 1500;
    //                                break;
    //                            case 1523:
    //                            case 1532:
    //                            case 1580:
    //                            case 1610:
    //                            case 1618:
    //                            case 1636:
    //                            case 1647:
    //                            case 1664:
    //                            case 3075:
    //                                quest.NextQuestId = quest.QuestId + 2;
    //                                break;
    //                            case 1527:
    //                            case 1553:
    //                                quest.NextQuestId = quest.QuestId + 3;
    //                                break;
    //                            case 1690:
    //                                quest.NextQuestId = 1694;
    //                                break;
    //                            case 1751:
    //                                quest.NextQuestId = 3000;
    //                                break;
    //                            case 3101:
    //                                quest.NextQuestId = 3200;
    //                                break;
    //                            case 3331:
    //                                quest.NextQuestId = 3340;
    //                                break;

    //                            default:
    //                                if (quest.QuestId < 1500 || quest.QuestId >= 1751 && quest.QuestId < 3000 || quest.QuestId >= 3374)
    //                                {
    //                                    continue;
    //                                }

    //                                quest.NextQuestId = quest.QuestId + 1;
    //                                break;
    //                        }

    //                        break;

    //                    case "DATA":
    //                        if (currentLine.Length < 3)
    //                        {
    //                            return;
    //                        }

    //                        objectiveIndex++;
    //                        int? data = null, objective = null, specialData = null, secondSpecialData = null;
    //                        switch ((QuestType)quest.QuestType)
    //                        {
    //                            case QuestType.Hunt:
    //                            case QuestType.Capture1:
    //                            case QuestType.Capture2:
    //                            case QuestType.Collect1:
    //                            case QuestType.Product:
    //                                data = int.Parse(currentLine[1]);
    //                                objective = int.Parse(currentLine[2]);
    //                                break;

    //                            case QuestType.Brings: // npcVNum - ItemCount - ItemVNum //
    //                            case QuestType.Collect3: // ItemVNum - Objective - TsId //
    //                            case QuestType.Needed: // ItemVNum - Objective - npcVNum //
    //                            case QuestType.Collect5: // ItemVNum - Objective - npcVNum //
    //                                data = int.Parse(currentLine[2]);
    //                                objective = int.Parse(currentLine[3]);
    //                                specialData = int.Parse(currentLine[1]);
    //                                break;

    //                            case QuestType.Collect4: // ItemVNum - Objective - MonsterVNum - DropRate // 
    //                            case QuestType.Collect2: // ItemVNum - Objective - MonsterVNum - DropRate // 
    //                                data = int.Parse(currentLine[2]);
    //                                objective = int.Parse(currentLine[3]);
    //                                specialData = int.Parse(currentLine[1]);
    //                                secondSpecialData = int.Parse(currentLine[4]);
    //                                break;

    //                            case QuestType.TimesSpace: // TS Lvl - Objective - TS Id //
    //                            case QuestType.TsPoint:
    //                                data = int.Parse(currentLine[4]);
    //                                objective = int.Parse(currentLine[2]);
    //                                specialData = int.Parse(currentLine[1]);
    //                                break;

    //                            case QuestType.Wear: // Item VNum - * - NpcVNum //
    //                                data = int.Parse(currentLine[2]);
    //                                specialData = int.Parse(currentLine[1]);
    //                                break;

    //                            case QuestType.TransmitGold: // NpcVNum - Gold x10K - * //
    //                                data = int.Parse(currentLine[1]);
    //                                objective = int.Parse(currentLine[2]) * 10000;
    //                                break;

    //                            case QuestType.GoTo: // Map - PosX - PosY //
    //                                data = int.Parse(currentLine[1]);
    //                                objective = int.Parse(currentLine[2]);
    //                                specialData = int.Parse(currentLine[3]);
    //                                break;

    //                            case QuestType.WinRaid: // Design - Objective - ? //
    //                                data = int.Parse(currentLine[1]);
    //                                objective = int.Parse(currentLine[2]);
    //                                specialData = int.Parse(currentLine[3]);
    //                                break;

    //                            case QuestType.Use: // Item to use - * - mateVnum //
    //                                data = int.Parse(currentLine[1]);
    //                                specialData = int.Parse(currentLine[2]);
    //                                break;

    //                            case QuestType.Dialog1: // npcVNum - * - * //
    //                            case QuestType.Dialog2: // npcVNum - * - * //
    //                                data = int.Parse(currentLine[1]);
    //                                break;

    //                            case QuestType.FlowerQuest:
    //                                objective = 10;
    //                                break;

    //                            case QuestType.Inspect: // NpcVNum - Objective - ItemVNum //
    //                            case QuestType.Required: // npcVNum - Objective - ItemVNum //
    //                                data = int.Parse(currentLine[1]);
    //                                objective = int.Parse(currentLine[3]);
    //                                specialData = int.Parse(currentLine[2]);
    //                                break;

    //                            default:
    //                                data = int.Parse(currentLine[1]);
    //                                objective = int.Parse(currentLine[2]);
    //                                specialData = int.Parse(currentLine[3]);
    //                                break;
    //                        }

    //                        currentObjectives.Add(new QuestObjectiveDTO
    //                        {
    //                            Data = data,
    //                            Objective = objective ?? 1,
    //                            SpecialData = specialData < 0 ? null : specialData,
    //                            DropRate = secondSpecialData < 0 ? null : specialData,
    //                            ObjectiveIndex = objectiveIndex,
    //                            QuestId = (int)quest.QuestId
    //                        });
    //                        break;

    //                    case "PRIZE":
    //                        for (int a = 1; a < 5; a++)
    //                        {
    //                            if (!dictionaryRewards.ContainsKey(long.Parse(currentLine[a])))
    //                            {
    //                                continue;
    //                            }

    //                            QuestRewardDTO currentReward = dictionaryRewards[long.Parse(currentLine[a])];
    //                            currentRewards.Add(currentReward);
    //                        }

    //                        break;
    //                }
    //            }
    //        }
    //    }
    //}
}
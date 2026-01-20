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
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers

{

    //BEGIN
    //VNUM    {QuestId}     {QuestType}   {autoFinish}    {Daily}	{requiredQuest}    {Secondary}
    //LEVEL   {LevelMin}	{LevelMax}
    //TITLE   {Title}
    //DESC    {Desc}
    //TALK    {StartDialogId}	{EndDialogId}	0	0
    //TARGET  {TargetX}    {TargetY}	{TargetMap}
    //DATA	  {FirstData}    {SecondData}	{ThirdData}     {FourthData}
    //PRIZE	  {firstPrizeVNUM}	{secondPrizeVNUM}	{thirdPrizeVNUM}	{fourthPrizeVNUM}
    //LINK	  {NextQuest}
    //END
    //
    //#=======

    public class QuestParser(IDao<QuestDto, short> questDao, IDao<QuestObjectiveDto, Guid> questObjectiveDao,
        IDao<QuestRewardDto, short> questRewardDao, IDao<QuestQuestRewardDto, Guid> questQuestRewardDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly string _fileQuestDat = $"{Path.DirectorySeparatorChar}quest.dat";
        private Dictionary<short, QuestRewardDto>? _questRewards;

        public async Task ImportQuestsAsync(string folder)
        {
            _questRewards = questRewardDao.LoadAll().ToDictionary(x => x.QuestRewardId, x => x);

            var parser = FluentParserBuilder<QuestDto>.Create(folder + _fileQuestDat, "END", 0)
                .Field(x => x.QuestId, chunk => Convert.ToInt16(chunk["VNUM"][0][1]))
                .Field(x => x.QuestType, chunk => (QuestType)Enum.Parse(typeof(QuestType), chunk["VNUM"][0][2]))
                .Field(x => x.AutoFinish, chunk => chunk["VNUM"][0][3] == "1")
                .Field(x => x.IsDaily, chunk => chunk["VNUM"][0][4] == "-1")
                .Field(x => x.RequiredQuestId, chunk => chunk["VNUM"][0][5] != "-1" ? short.Parse(chunk["VNUM"][0][5]) : (short?)null)
                .Field(x => x.IsSecondary, chunk => chunk["VNUM"][0][6] != "-1")
                .Field(x => x.LevelMin, chunk => Convert.ToByte(chunk["LEVEL"][0][1]))
                .Field(x => x.LevelMax, chunk => Convert.ToByte(chunk["LEVEL"][0][2]))
                .Field(x => x.TitleI18NKey, chunk => chunk["TITLE"][0][1])
                .Field(x => x.DescI18NKey, chunk => chunk["DESC"][0][1])
                .Field(x => x.TargetX, chunk => chunk["TARGET"][0][1] == "-1" ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][1]))
                .Field(x => x.TargetY, chunk => chunk["TARGET"][0][2] == "-1" ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][2]))
                .Field(x => x.TargetMap, chunk => chunk["TARGET"][0][3] == "-1" ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][3]))
                .Field(x => x.StartDialogId, chunk => chunk["TARGET"][0][1] == "-1" ? (int?)null : Convert.ToInt32(chunk["TALK"][0][1]))
                .Field(x => x.EndDialogId, chunk => chunk["TARGET"][0][2] == "-1" ? (int?)null : Convert.ToInt32(chunk["TALK"][0][2]))
                .Field(x => x.NextQuestId, chunk => chunk["LINK"][0][1] == "-1" ? (short?)null : Convert.ToInt16(chunk["LINK"][0][1]))
                .Field(x => x.QuestQuestReward, chunk => ImportQuestQuestRewards(chunk))
                .Field(x => x.QuestObjective, chunk => ImportQuestObjectives(chunk))
                .Build(logger, logLanguage);
            var quests = await parser.GetDtosAsync();

            await questDao.TryInsertOrUpdateAsync(quests);
            await questQuestRewardDao.TryInsertOrUpdateAsync(quests.Where(s => s.QuestQuestReward != null).SelectMany(s => s.QuestQuestReward));
            await questObjectiveDao.TryInsertOrUpdateAsync(quests.Where(s => s.QuestObjective != null).SelectMany(s => s.QuestObjective));

            logger.Information(logLanguage[LogLanguageKey.QUESTS_PARSED], quests.Count);
        }

        private List<QuestQuestRewardDto> ImportQuestQuestRewards(Dictionary<string, string[][]> chunk)
        {
            var currentRewards = new List<QuestQuestRewardDto>();
            for (var i = 1; i < 5; i++)
            {
                var prize = Convert.ToInt16(chunk["PRIZE"][0][i]);
                if ((prize == -1) || (_questRewards?.ContainsKey(prize) == false))
                {
                    continue;
                }

                currentRewards.Add(new QuestQuestRewardDto
                {
                    Id = Guid.NewGuid(),
                    QuestId = Convert.ToInt16(chunk["VNUM"][0][1]),
                    QuestRewardId = prize,
                });
            }

            return currentRewards;
        }

        private List<QuestObjectiveDto> ImportQuestObjectives(Dictionary<string, string[][]> chunk)
        {
            var objectivsDtos = new List<QuestObjectiveDto>();
            foreach (var line in chunk["DATA"])
            {
                if (line[1] != "-1")
                {
                    objectivsDtos.Add(new QuestObjectiveDto
                    {
                        QuestId = Convert.ToInt16(chunk["VNUM"][0][1]),
                        FirstData = Convert.ToInt32(line[1]),
                        SecondData = line[2] == "-1" ? (int?)null : Convert.ToInt32(line[2]),
                        ThirdData = line[3] == "-1" ? (int?)null : Convert.ToInt32(line[3]),
                        FourthData = line[4] == "-1" ? (int?)null : Convert.ToInt32(line[4]),
                        QuestObjectiveId = Guid.NewGuid()
                    });
                }
            }
            return objectivsDtos;
        }
    }
}

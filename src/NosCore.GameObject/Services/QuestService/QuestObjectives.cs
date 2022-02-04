using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using System;

namespace NosCore.GameObject.Services.QuestService
{
    public class QuestObjectives : QuestObjectiveDto
    {
        public int? GetMaxCount(QuestType questType)
        {
            return questType switch
            {
                QuestType.Hunt or QuestType.SpecialCollect or QuestType.TimesSpace or QuestType.Product => SecondData != null && ThirdData != null ? (int?)Math.Floor((double)((SecondData + ThirdData) / 2)) : SecondData == null ? ThirdData : SecondData,
                QuestType.Brings or QuestType.CaptureWithoutGettingTheMonster or QuestType.Capture  => ThirdData != null && FourthData != null ? (int?)Math.Floor((double)((ThirdData + FourthData) / 2)) : ThirdData == null ? FourthData : ThirdData,
                QuestType.CollectInRaid or QuestType.CollectInTs or QuestType.Required or QuestType.Needed or QuestType.Collect or QuestType.CollectMapEntity or QuestType.Inspect => ThirdData,
                QuestType.TsPoint => FourthData,
                QuestType.TransmitGold => SecondData * 10000,
                QuestType.WinRaid => SecondData,
                QuestType.FlowerQuest => FirstData,
                _ => 0,
            };
        }
    }
}

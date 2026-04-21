//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Quest;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.QuestService
{
    public class CharacterQuest : CharacterQuestDto
    {
        public Quest Quest { get; set; } = null!;

        public ConcurrentDictionary<System.Guid, int> ObjectiveProgress { get; } = new();

        public QstiPacket GenerateQstiPacket(bool showDialog)
        {
            return new QstiPacket(GenerateQuestSubPacket(showDialog));
        }

        public QuestSubPacket GenerateQuestSubPacket(bool showDialog)
        {
            var objectives = new List<QuestObjectiveSubPacket>();
            var questCount = 0;
            foreach (var objective in Quest.QuestObjectives)
            {
                var maxCount = IsCountableObjective(Quest.QuestType) ? (short)(objective.SecondData ?? 0) : (short)0;
                var currentCount = IsCountableObjective(Quest.QuestType)
                    && ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var c)
                    ? (short)c
                    : (short)0;
                objectives.Add(new QuestObjectiveSubPacket
                {
                    CurrentCount = currentCount,
                    MaxCount = maxCount,
                    IsFinished = questCount == 0 ? CompletedOn != null : (bool?)null
                });
                questCount++;
            }

            return new QuestSubPacket
            {
                QuestId = QuestId,
                InfoId = QuestId,
                GoalType = Quest.QuestType,
                ObjectiveCount = 5,
                ShowDialog = showDialog,
                QuestObjectiveSubPackets = objectives
            };
        }

        private static bool IsCountableObjective(Packets.Enumerations.QuestType type) => type switch
        {
            Packets.Enumerations.QuestType.Hunt => true,
            Packets.Enumerations.QuestType.NumberOfKill => true,
            _ => false
        };

    }

    public class Quest : QuestDto
    {
        public TargetPacket GenerateTargetPacket()
        {
            return new TargetPacket
            {
                QuestId = QuestId, TargetMap = TargetMap ?? 0, TargetX = TargetX ?? 0,
                TargetY = TargetY ?? 0
            };
        }

        public TargetOffPacket GenerateTargetOffPacket()
        {
            return new TargetOffPacket
            {
                QuestId = QuestId, TargetMap = TargetMap ?? 0, TargetX = TargetX ?? 0,
                TargetY = TargetY ?? 0
            };
        }

        public List<QuestObjectiveDto> QuestObjectives { get; set; } = null!;
    }
}

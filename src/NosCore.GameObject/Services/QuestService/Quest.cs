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
using System.Linq;

namespace NosCore.GameObject.Services.QuestService
{
    public class CharacterQuest : CharacterQuestDto
    {
        public required Quest Quest { get; set; }

        public ConcurrentDictionary<System.Guid, int> ObjectiveProgress { get; } = new();

        public NodaTime.Instant? ObjectivesCompletedOn { get; set; }

        public byte Slot { get; set; }

        // Main quests take the first five quest window slots and secondary quests the last five. A quest that follows
        // another through its link keeps that quest's slot.
        public static byte FreeSlot(IEnumerable<CharacterQuest> quests, bool secondary, byte? previous = null)
        {
            var taken = TakenSlots(quests);
            var candidates = previous is { } slot ? new[] { (int)slot }.Concat(SlotRange(secondary)) : SlotRange(secondary);
            return (byte)candidates.Concat(Enumerable.Range(0, 10)).FirstOrDefault(s => !taken.Contains((byte)s));
        }

        public static bool HasFreeSlot(IEnumerable<CharacterQuest> quests, bool secondary)
        {
            var taken = TakenSlots(quests);
            return SlotRange(secondary).Any(s => !taken.Contains((byte)s));
        }

        private static HashSet<byte> TakenSlots(IEnumerable<CharacterQuest> quests) =>
            quests.Where(q => q.CompletedOn == null).Select(q => q.Slot).ToHashSet();

        private static IEnumerable<int> SlotRange(bool secondary) => Enumerable.Range(secondary ? 5 : 0, 5);

        // A daily quest can be taken again once a day has passed, so it only counts as done until then.
        public bool IsDoneAt(NodaTime.Instant now) =>
            CompletedOn is { } done && (!Quest.IsDaily || done.Plus(NodaTime.Duration.FromDays(1)) > now);

        public QstiPacket GenerateQstiPacket(bool showDialog)
        {
            return new QstiPacket(GenerateQuestSubPacket(showDialog));
        }

        public bool AreObjectivesComplete()
        {
            if (!IsCountableObjective(Quest.QuestType))
            {
                return ObjectivesCompletedOn != null;
            }
            return Quest.QuestObjectives.All(o =>
            {
                var required = o.RequiredAmount(Quest.QuestType);
                if (required <= 0) return true;
                return ObjectiveProgress.TryGetValue(o.QuestObjectiveId, out var c) && c >= required;
            });
        }

        public QuestSubPacket GenerateQuestSubPacket(bool showDialog)
        {
            var pairs = new (short Cur, short Req)[5];
            var countable = IsCountableObjective(Quest.QuestType);
            for (var i = 0; i < Quest.QuestObjectives.Count && i < 5; i++)
            {
                var objective = Quest.QuestObjectives[i];
                var required = countable ? (short)objective.RequiredAmount(Quest.QuestType) : (short)0;
                var current = countable
                    && ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var c)
                    ? (short)c
                    : (short)0;
                pairs[i] = (current, required);
            }

            return new QuestSubPacket
            {
                QuestSlot = Slot,
                QuestId = QuestId,
                QuestLineId = Quest.IsSecondary ? (short)0 : Quest.QuestLineId,
                QuestType = Quest.QuestType,
                Status = (byte)(AreObjectivesComplete() ? 1 : 0),
                Objective1Current = pairs[0].Cur,
                Objective1Required = pairs[0].Req,
                Objective2Current = pairs[1].Cur,
                Objective2Required = pairs[1].Req,
                Objective3Current = pairs[2].Cur,
                Objective3Required = pairs[2].Req,
                Objective4Current = pairs[3].Cur,
                Objective4Required = pairs[3].Req,
                Objective5Current = pairs[4].Cur,
                Objective5Required = pairs[4].Req,
                QuestToShowInfo = showDialog
            };
        }

        private static bool IsCountableObjective(Packets.Enumerations.QuestType type) => type switch
        {
            Packets.Enumerations.QuestType.Hunt => true,
            Packets.Enumerations.QuestType.NumberOfKill => true,
            Packets.Enumerations.QuestType.Capture => true,
            Packets.Enumerations.QuestType.CaptureWithoutGettingTheMonster => true,
            Packets.Enumerations.QuestType.Brings => true,
            Packets.Enumerations.QuestType.Product => true,
            Packets.Enumerations.QuestType.TsPoint => true,
            Packets.Enumerations.QuestType.CollectInTs => true,
            Packets.Enumerations.QuestType.Required => true,
            Packets.Enumerations.QuestType.Collect => true,
            Packets.Enumerations.QuestType.TransmitGold => true,
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

        public required List<QuestObjectiveDto> QuestObjectives { get; set; }
    }
}

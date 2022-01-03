using NodaTime;
using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class BattlepassQuest : IEntity
    {
        [Key]
        public long Id { get; set; }

        public MissionType MissionType { get; set; }

        public FrequencyType FrequencyType { get; set; }

        public short Data { get; set; }

        public long MinObjectiveValue { get; set; }

        public long MaxObjectiveValue { get; set; }

        public short RewardAmount { get; set; }

        public Instant Start { get; set; }
    }
}

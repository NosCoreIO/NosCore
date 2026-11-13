//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class FamilyQuestProgress : IEntity
    {
        [Key]
        public long FamilyQuestProgressId { get; set; }

        public virtual Family Family { get; set; }

        public long FamilyId { get; set; }

        public short QuestId { get; set; }

        public int Progress { get; set; }

        public Instant? CompletedAt { get; set; }

        public int TotalCompletions { get; set; }

        public Instant? ProgressedAt { get; set; }
    }
}

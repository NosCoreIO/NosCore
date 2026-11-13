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
    // When a member last counted towards a family quest that takes one count per member a day.
    public class FamilyQuestContribution : IEntity
    {
        [Key]
        public long FamilyQuestContributionId { get; set; }

        public virtual Family Family { get; set; }

        public long FamilyId { get; set; }

        public short QuestId { get; set; }

        public long CharacterId { get; set; }

        public Instant ContributedAt { get; set; }
    }
}

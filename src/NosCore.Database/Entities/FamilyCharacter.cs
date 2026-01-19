//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Family;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class FamilyCharacter : IEntity
    {
        public FamilyAuthority Authority { get; set; }

        public virtual Character Character { get; set; } = null!;

        public long CharacterId { get; set; }

        [MaxLength(255)]
        public string? DailyMessage { get; set; }

        public int Experience { get; set; }

        public virtual Family Family { get; set; } = null!;

        [Key]
        public long FamilyCharacterId { get; set; }

        public long FamilyId { get; set; }

        public FamilyMemberRank Rank { get; set; }
    }
}

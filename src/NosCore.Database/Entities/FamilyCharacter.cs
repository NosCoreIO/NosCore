using System.ComponentModel.DataAnnotations;
using NosCore.Shared.Enumerations.Family;

namespace NosCore.Database.Entities
{
    public class FamilyCharacter
    {
        #region Properties

        public FamilyAuthority Authority { get; set; }

        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        [MaxLength(255)]
        public string DailyMessage { get; set; }

        public int Experience { get; set; }

        public virtual Family Family { get; set; }

        public long FamilyCharacterId { get; set; }

        public long FamilyId { get; set; }

        public FamilyMemberRank Rank { get; set; }

        #endregion
    }
}
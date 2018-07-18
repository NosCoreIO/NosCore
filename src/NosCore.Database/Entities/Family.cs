using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Family;

namespace NosCore.Database.Entities
{
    public class Family
    {
        #region Instantiation

        public Family()
        {
            FamilyCharacters = new HashSet<FamilyCharacter>();
            FamilyLogs = new HashSet<FamilyLog>();
        }

        #endregion

        #region Properties

        public virtual ICollection<FamilyCharacter> FamilyCharacters { get; set; }

        public int FamilyExperience { get; set; }

        public GenderType FamilyHeadGender { get; set; }

        public long FamilyId { get; set; }

        public byte FamilyLevel { get; set; }

        public byte FamilyFaction { get; set; }

        public virtual ICollection<FamilyLog> FamilyLogs { get; set; }

        [MaxLength(255)]
        public string FamilyMessage { get; set; }

        public FamilyAuthorityType ManagerAuthorityType { get; set; }

        public bool ManagerCanGetHistory { get; set; }

        public bool ManagerCanInvite { get; set; }

        public bool ManagerCanNotice { get; set; }

        public bool ManagerCanShout { get; set; }

        public byte MaxSize { get; set; }

        public FamilyAuthorityType MemberAuthorityType { get; set; }

        public bool MemberCanGetHistory { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public byte WarehouseSize { get; set; }

        #endregion
    }
}
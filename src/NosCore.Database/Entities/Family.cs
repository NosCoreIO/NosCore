//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Family : IEntity
    {
        public Family()
        {
            FamilyCharacters = new HashSet<FamilyCharacter>();
            FamilyLogs = new HashSet<FamilyLog>();
            Warehouses = new HashSet<Warehouse>();
        }

        public virtual ICollection<FamilyCharacter> FamilyCharacters { get; set; }

        public int FamilyExperience { get; set; }

        public GenderType FamilyHeadGender { get; set; }

        [Key]
        public long FamilyId { get; set; }

        public byte FamilyLevel { get; set; }

        public byte FamilyFaction { get; set; }

        public virtual ICollection<FamilyLog> FamilyLogs { get; set; }

        [MaxLength(255)]
        public string? FamilyMessage { get; set; }

        public FamilyAuthorityType ManagerAuthorityType { get; set; }

        public bool ManagerCanGetHistory { get; set; }

        public bool ManagerCanInvite { get; set; }

        public bool ManagerCanNotice { get; set; }

        public bool ManagerCanShout { get; set; }

        public byte MaxSize { get; set; }

        public FamilyAuthorityType MemberAuthorityType { get; set; }

        public bool MemberCanGetHistory { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Name { get; set; }

        public byte WarehouseSize { get; set; }

        public virtual ICollection<Warehouse> Warehouses { get; set; }
    }
}

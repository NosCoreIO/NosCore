//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Account : IEntity
    {
        public Account()
        {
            Character = new HashSet<Character>();
            PenaltyLog = new HashSet<PenaltyLog>();
        }

        [Key]
        [Required]
        public long AccountId { get; set; }

        [Required]
        public AuthorityType Authority { get; set; }

        public virtual ICollection<Character> Character { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Name { get; set; }

        [MaxLength(255)]
        public string? Password { get; set; }

        [MaxLength(255)]
        public string? NewAuthPassword { get; set; }

        [MaxLength(255)]
        public string? NewAuthSalt { get; set; }

        public virtual ICollection<PenaltyLog> PenaltyLog { get; set; }

        [MaxLength(45)]
        public string? RegistrationIp { get; set; }

        [MaxLength(32)]
        public string? VerificationToken { get; set; }

        public RegionType Language { get; set; }

        public long BankMoney { get; set; }

        public long ItemShopMoney { get; set; }

        [MaxLength(255)]
        public string? MfaSecret { get; set; }
    }
}

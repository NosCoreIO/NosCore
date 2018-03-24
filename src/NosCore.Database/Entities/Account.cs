using NosCore.Domain;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Account
    {
        #region Instantiation

        public Account()
        {
            Character = new HashSet<Character>();
            PenaltyLog = new HashSet<PenaltyLog>();
        }

        #endregion

        #region Properties
        public long AccountId { get; set; }

        public AuthorityType Authority { get; set; }

        public virtual ICollection<Character> Character { get; set; }

        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Password { get; set; }

        public virtual ICollection<PenaltyLog> PenaltyLog { get; set; }

        [MaxLength(45)]
        public string RegistrationIP { get; set; }

        [MaxLength(32)]
        public string VerificationToken { get; set; }

        #endregion
    }
}
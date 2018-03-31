using NosCore.Domain;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Data
{
    public class AccountDTO : IDTO
    {
        [Key]
        public long AccountId { get; set; }

        public AuthorityType Authority { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public string RegistrationIP { get; set; }

        public string VerificationToken { get; set; }

        public long BankMoney { get; set; }

        public long ItemShopMoney { get; set; }

        public RegionType RegionType { get; set; }
    }
}
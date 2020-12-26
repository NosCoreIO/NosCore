//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NosCore.Database.Entities.Base;
using NosCore.Shared.Enumerations;

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
        public string Name { get; set; } = "";

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
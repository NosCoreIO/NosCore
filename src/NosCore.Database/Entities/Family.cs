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
        public string Name { get; set; } = "";

        public byte WarehouseSize { get; set; }

        public virtual ICollection<Warehouse> Warehouses { get; set; }
    }
}
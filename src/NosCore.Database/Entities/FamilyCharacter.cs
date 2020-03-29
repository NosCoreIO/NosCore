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

using System.ComponentModel.DataAnnotations;
using NosCore.Data.Enumerations.Family;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class FamilyCharacter : IEntity
    {
        public FamilyAuthority Authority { get; set; }

        public virtual Character Character { get; set; } = new Character();

        public long CharacterId { get; set; }

        [MaxLength(255)]
        public string? DailyMessage { get; set; }

        public int Experience { get; set; }

        public virtual Family Family { get; set; } = new Family();

        [Key]
        public long FamilyCharacterId { get; set; }

        public long FamilyId { get; set; }

        public FamilyMemberRank Rank { get; set; }
    }
}
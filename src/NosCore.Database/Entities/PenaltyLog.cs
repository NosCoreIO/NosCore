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

using System;
using System.ComponentModel.DataAnnotations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class PenaltyLog : IEntity
    {
        public virtual Account Account { get; set; } = null!;

        public long AccountId { get; set; }

        [Required]
        public string AdminName { get; set; } = "";

        public DateTime DateEnd { get; set; }

        public DateTime DateStart { get; set; }

        public PenaltyType Penalty { get; set; }

        [Key]
        public int PenaltyLogId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Reason { get; set; } = "";
    }
}
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
using System;
using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace NosCore.Database.Entities
{
    public class BazaarItem : IEntity
    {
        public virtual Character Seller { get; set; } = null!;

        public short Amount { get; set; }

        [Key]
        public long BazaarItemId { get; set; }

        public Instant DateStart { get; set; }

        public short Duration { get; set; }

        public bool IsPackage { get; set; }

        public virtual ItemInstance ItemInstance { get; set; } = null!;

        public Guid ItemInstanceId { get; set; }

        public bool MedalUsed { get; set; }

        public long Price { get; set; }

        public long SellerId { get; set; }
    }
}
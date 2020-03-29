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
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class RollGeneratedItem : IEntity
    {
        [Key]
        public short RollGeneratedItemId { get; set; }

        public short OriginalItemDesign { get; set; }

        public virtual Item OriginalItem { get; set; } = null!;

        public short OriginalItemVNum { get; set; }

        public short Probability { get; set; }

        public byte ItemGeneratedAmount { get; set; }

        public short ItemGeneratedVNum { get; set; }

        public byte ItemGeneratedUpgrade { get; set; }

        public bool IsRareRandom { get; set; }

        public short MinimumOriginalItemRare { get; set; }

        public short MaximumOriginalItemRare { get; set; }

        public virtual Item ItemGenerated { get; set; } = null!;

        public bool IsSuperReward { get; set; }
    }
}
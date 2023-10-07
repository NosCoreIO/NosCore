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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static NosCore.Data.Enumerations.Buff.BCardType;

namespace NosCore.Database.Entities
{
    public class Card : IStaticEntity
    {
        public Card()
        {
            BCards = new HashSet<BCard>();
            StaticBuff = new HashSet<StaticBuff>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short CardId { get; set; }

        public int Duration { get; set; }

        public int EffectId { get; set; }

        public byte Level { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NBCard))]
        public required string Name { get; set; } 

        public int Delay { get; set; }

        public short TimeoutBuff { get; set; }

        public byte TimeoutBuffChance { get; set; }

        public CardType BuffType { get; set; }

        public byte Propability { get; set; }

        public virtual ICollection<BCard> BCards { get; set; }

        public virtual ICollection<StaticBuff> StaticBuff { get; set; }
    }
}
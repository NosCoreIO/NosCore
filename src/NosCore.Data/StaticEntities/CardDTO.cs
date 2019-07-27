﻿//  __  _  __    __   ___ __  ___ ___  
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
using Mapster;
using NosCore.Data.I18N;
using BCardType = NosCore.Data.Enumerations.Buff.BCardType;

namespace NosCore.Data.StaticEntities
{
    public class CardDto : IStaticDto
    {
        [Key]
        public short CardId { get; set; }

        public int Duration { get; set; }

        public int EffectId { get; set; }

        public byte Level { get; set; }

        public int Delay { get; set; }

        public short TimeoutBuff { get; set; }

        public byte TimeoutBuffChance { get; set; }

        public BCardType.CardType BuffType { get; set; }

        public byte Propability { get; set; }

        public ICollection<BCardDto> BCards { get; set; }

        [I18NFrom(typeof(I18NCardDto))]
         public I18NString Name { get; set; } = new I18NString();

        [AdaptMember("Name")]
        public string NameI18NKey { get; set; }
    }
}
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
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class BCard : IStaticEntity
    {
        [Key]
        public short BCardId { get; set; }

        public byte SubType { get; set; }

        public byte Type { get; set; }

        public int FirstData { get; set; }

        public int SecondData { get; set; }

        public virtual Card? Card { get; set; }

        public virtual Item? Item { get; set; }

        public virtual Skill? Skill { get; set; }

        public virtual NpcMonster? NpcMonster { get; set; }

        public short? CardId { get; set; }

        public short? ItemVNum { get; set; }

        public short? SkillVNum { get; set; }

        public short? NpcMonsterVNum { get; set; }

        public byte CastType { get; set; }

        public int ThirdData { get; set; }

        public bool IsLevelScaled { get; set; }

        public bool IsLevelDivided { get; set; }
    }
}
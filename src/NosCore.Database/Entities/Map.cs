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

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.MAPS_LOADED, EmptyMessage = LogLanguageKey.NO_MAP)]
    public class Map : IStaticEntity
    {
        public Map()
        {
            Character = new HashSet<Character>();
            MapMonster = new HashSet<MapMonster>();
            MapNpc = new HashSet<MapNpc>();
            Portal = new HashSet<Portal>();
            Portal1 = new HashSet<Portal>();
            ScriptedInstance = new HashSet<ScriptedInstance>();
            Teleporter = new HashSet<Teleporter>();
            MapTypeMap = new HashSet<MapTypeMap>();
            Respawn = new HashSet<Respawn>();
            RespawnMapType = new HashSet<RespawnMapType>();
        }

        public virtual ICollection<Character> Character { get; set; }

        public byte[] Data { get; set; } = null!;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short MapId { get; set; }

        public virtual ICollection<MapMonster> MapMonster { get; set; }

        public virtual ICollection<MapNpc> MapNpc { get; set; }

        public virtual ICollection<MapTypeMap> MapTypeMap { get; set; }

        public int Music { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NMapIdData))]
        public string Name { get; set; } = "";

        public virtual ICollection<Portal> Portal { get; set; }

        public virtual ICollection<Portal> Portal1 { get; set; }

        public virtual ICollection<Respawn> Respawn { get; set; }

        public virtual ICollection<RespawnMapType> RespawnMapType { get; set; }

        public virtual ICollection<ScriptedInstance> ScriptedInstance { get; set; }

        public bool ShopAllowed { get; set; }

        public virtual ICollection<Teleporter> Teleporter { get; set; }
    }
}
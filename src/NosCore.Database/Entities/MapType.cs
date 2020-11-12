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
using System.ComponentModel.DataAnnotations.Schema;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class MapType : IStaticEntity
    {
        public MapType()
        {
            MapTypeMap = new HashSet<MapTypeMap>();
            Drops = new HashSet<Drop>();
        }

        public virtual ICollection<Drop> Drops { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short MapTypeId { get; set; }

        public virtual ICollection<MapTypeMap> MapTypeMap { get; set; }

        [Required]
        public string MapTypeName { get; set; } = "";

        public short PotionDelay { get; set; }

        public virtual RespawnMapType? RespawnMapType { get; set; }

        public long? RespawnMapTypeId { get; set; }

        public virtual RespawnMapType? ReturnMapType { get; set; }

        public long? ReturnMapTypeId { get; set; }
    }
}
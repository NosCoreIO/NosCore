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
    public class RespawnMapType : IStaticEntity
    {
        public RespawnMapType()
        {
            Respawn = new HashSet<Respawn>();
            MapTypes = new HashSet<MapType>();
            MapTypes1 = new HashSet<MapType>();
        }

        public short MapId { get; set; }

        public short DefaultX { get; set; }

        public short DefaultY { get; set; }

        public virtual Map Map { get; set; } = new Map();

        public virtual ICollection<MapType> MapTypes { get; set; }

        public virtual ICollection<MapType> MapTypes1 { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        public virtual ICollection<Respawn> Respawn { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long RespawnMapTypeId { get; set; }
    }
}
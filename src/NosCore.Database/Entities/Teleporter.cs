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
using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class Teleporter : IStaticEntity
    {
        public short Index { get; set; }

        public TeleporterType Type { get; set; }

        public virtual Map Map { get; set; } = new Map();

        public short MapId { get; set; }

        public virtual MapNpc MapNpc { get; set; } = new MapNpc();

        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        [Key]
        public short TeleporterId { get; set; }
    }
}
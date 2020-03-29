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
    public class ScriptedInstance : IEntity
    {
        public virtual Map Map { get; set; } = new Map();

        public short MapId { get; set; }

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public string? Label { get; set; }

        [MaxLength(int.MaxValue)]
        public string? Script { get; set; }

        [Key]
        public short ScriptedInstanceId { get; set; }

        public ScriptedInstanceType Type { get; set; }
    }
}
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
using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.SCRIPTS_LOADED)]
    public class Script : SynchronizableBaseEntity, IStaticEntity
    {
        public Script()
        {
            Characters = new HashSet<Character>();
        }

        public virtual ICollection<Character> Characters { get; set; }

        public byte ScriptId { get; set; }

        public short ScriptStepId { get; set; }

        [Required]
        public string StepType { get; set; } = null!;

        public string? StringArgument { get; set; }

        public short? Argument1 { get; set; }

        public short? Argument2 { get; set; }

        public short? Argument3 { get; set; }
    }
}
//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

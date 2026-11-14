//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    /// <summary>A story chapter an NPC hands out from its "Get Quest" list.</summary>
    [StaticMetaData(LoadedMessage = LogLanguageKey.SCRIPT_OFFERS_LOADED)]
    public class ScriptOffer : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte ScriptId { get; set; }

        public short NpcVNum { get; set; }

        /// <summary>The chapter that has to be finished first.</summary>
        public byte RequiredScriptId { get; set; }

        public byte LevelMinimum { get; set; }

        /// <summary>The NPC dialog whose buttons list the chapters.</summary>
        public short DialogId { get; set; }
    }
}

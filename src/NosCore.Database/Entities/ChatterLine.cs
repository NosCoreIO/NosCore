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
    /// <summary>A line a monster of the chatter group shouts when it notices an enemy.</summary>
    [StaticMetaData(LoadedMessage = LogLanguageKey.CHATTER_LINES_LOADED)]
    public class ChatterLine : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short ChatterLineId { get; set; }

        public byte ChatterGroup { get; set; }

        /// <summary>A Game18NConstString id.</summary>
        public short Message { get; set; }
    }
}

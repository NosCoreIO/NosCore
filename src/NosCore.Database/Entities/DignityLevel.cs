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
    [StaticMetaData(LoadedMessage = LogLanguageKey.DIGNITYLEVELS_LOADED, EmptyMessage = LogLanguageKey.NO_DIGNITYLEVEL)]
    public class DignityLevel : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte DignityLevelId { get; set; }

        // Dignity descends: this is the highest value still inside the band.
        public short? MaxDignity { get; set; }
    }
}

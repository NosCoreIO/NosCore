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
    [StaticMetaData(LoadedMessage = LogLanguageKey.DIGNITYLEVELS_LOADED)]
    public class DignityLevel : IStaticEntity
    {
        // Matches DignityType, whose values 1..6 are the client's dignity bands in order.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte DignityLevelId { get; set; }

        // Dignity descends, so this is the highest value still inside the band. Null on Default,
        // which the client bounds at 0 but which has to keep catching everything above the first
        // penalty band: the client declares nothing between -1 and -99.
        public short? MaxDignity { get; set; }
    }
}

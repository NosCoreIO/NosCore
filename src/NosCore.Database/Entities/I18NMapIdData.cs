//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using NosCore.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class I18NMapIdData : IEntity
    {
        [Key]
        public int I18NMapIdDataId { get; set; }

        [Required]
        public required string Key { get; set; }

        public RegionType RegionType { get; set; }

        [Required]
        public required string Text { get; set; }
    }
}

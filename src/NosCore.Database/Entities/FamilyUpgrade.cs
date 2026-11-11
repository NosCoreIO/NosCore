//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.FAMILY_UPGRADES_LOADED)]
    public class FamilyUpgrade : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short ItemVNum { get; set; }

        public virtual Item Item { get; set; }

        public byte FamilyLevelMinimum { get; set; }

        public FamilyUpgradeType UpgradeType { get; set; }

        /// <summary>0 for the first purchase of a line, 1 for the one that requires it.</summary>
        public byte UpgradeLevel { get; set; }
    }
}

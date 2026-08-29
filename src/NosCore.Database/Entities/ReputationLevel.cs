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
    [StaticMetaData(LoadedMessage = LogLanguageKey.REPUTATIONLEVELS_LOADED)]
    public class ReputationLevel : IStaticEntity
    {
        // Matches ReputationType, whose values 1..27 are the client's reputation bands in order.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte ReputationLevelId { get; set; }

        public long MinReputation { get; set; }

        // Null on the highest band only, which the client states as an open range.
        public long? MaxReputation { get; set; }
    }
}

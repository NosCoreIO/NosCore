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
    [StaticMetaData(LoadedMessage = LogLanguageKey.NPCMONSTERSTALKS_LOADED)]
    public class NpcTalk : IStaticEntity
    {
        public NpcTalk()
        {
            MapNpc = new HashSet<MapNpc>();
        }

        [Key]
        public short DialogId { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NNpcMonsterTalk))]
        public required string Name { get; set; }

        public virtual ICollection<MapNpc> MapNpc { get; set; }
    }
}

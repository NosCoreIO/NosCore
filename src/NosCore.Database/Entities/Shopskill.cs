//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.SHOPSKILLS_LOADED)]
    public class ShopSkill : IStaticEntity
    {
        public virtual Shop Shop { get; set; } = null!;

        public int ShopId { get; set; }

        [Key]
        public int ShopSkillId { get; set; }

        public virtual Skill Skill { get; set; } = null!;

        public short SkillVNum { get; set; }

        public byte Slot { get; set; }

        public byte Type { get; set; }
    }
}

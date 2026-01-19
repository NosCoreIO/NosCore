//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Combo : IStaticEntity
    {
        public short Animation { get; set; }

        [Key]
        public int ComboId { get; set; }

        public short Effect { get; set; }

        public short Hit { get; set; }

        public virtual Skill Skill { get; set; } = null!;

        public short SkillVNum { get; set; }
    }
}

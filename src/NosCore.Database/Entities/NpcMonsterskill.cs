//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class NpcMonsterSkill : IStaticEntity
    {
        public virtual NpcMonster NpcMonster { get; set; } = null!;

        [Key]
        public long NpcMonsterSkillId { get; set; }

        public short NpcMonsterVNum { get; set; }

        public short Rate { get; set; }

        public virtual Skill Skill { get; set; } = null!;

        public short SkillVNum { get; set; }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class CharacterSkill : SynchronizableBaseEntity
    {
        public virtual Character Character { get; set; } = null!;

        public long CharacterId { get; set; }

        public virtual Skill Skill { get; set; } = null!;

        public short SkillVNum { get; set; }
    }
}

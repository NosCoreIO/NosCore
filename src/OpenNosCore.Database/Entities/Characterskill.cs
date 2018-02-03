using OpenNosCore.Database.Entities.Base;

namespace OpenNosCore.Database.Entities
{
    public class CharacterSkill : SynchronizableBaseEntity
    {
        #region Properties

        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public virtual Skill Skill { get; set; }

        public short SkillVNum { get; set; }

        #endregion
    }
}
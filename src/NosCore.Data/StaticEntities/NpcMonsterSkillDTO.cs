using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
    public class NpcMonsterSkillDTO : IDTO
    {
        [Key]
        public long NpcMonsterSkillId { get; set; }

        public short NpcMonsterVNum { get; set; }

        public short Rate { get; set; }

        public short SkillVNum { get; set; }
    }
}
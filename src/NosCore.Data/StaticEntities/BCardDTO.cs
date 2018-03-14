using NosCore.Database;
using NosCore.Domain.Buff;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Data
{
    public class BCardDTO : IDatabaseObject
    {
        [Key]
        public short BCardId { get; set; }

        public byte SubType { get; set; }

        public byte Type { get; set; }

        public int FirstData { get; set; }

        public int SecondData { get; set; }

        public virtual CardDTO Card { get; set; }

        public virtual AdditionalTypes.Item Item { get; set; }

        public virtual SkillDTO Skill { get; set; }

        public virtual NpcMonsterDTO NpcMonster { get; set; }

        public short? CardId { get; set; }

        public short? ItemVNum { get; set; }

        public short? SkillVNum { get; set; }

        public short? NpcMonsterVNum { get; set; }

        public byte CastType { get; set; }

        public int ThirdData { get; set; }

        public bool IsLevelScaled { get; set; }

        public bool IsLevelDivided { get; set; }
        public void Initialize()
        {
            
        }

    }
}

using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
    public class BCardDTO : IDTO
    {
        [Key]
        public short BCardId { get; set; }

        public byte SubType { get; set; }

        public byte Type { get; set; }

        public int FirstData { get; set; }

        public int SecondData { get; set; }

        public short? CardId { get; set; }

        public short? ItemVNum { get; set; }

        public short? SkillVNum { get; set; }

        public short? NpcMonsterVNum { get; set; }

        public byte CastType { get; set; }

        public int ThirdData { get; set; }

        public bool IsLevelScaled { get; set; }

        public bool IsLevelDivided { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
    public class ComboDTO : IDTO
    {
        public short Animation { get; set; }

        [Key]
        public int ComboId { get; set; }

        public short Effect { get; set; }

        public short Hit { get; set; }

        public short SkillVNum { get; set; }
    }
}
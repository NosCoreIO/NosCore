namespace NosCore.Database.Entities
{
    public class ShopSkill
    {
        #region Properties

        public virtual Shop Shop { get; set; }

        public int ShopId { get; set; }

        public int ShopSkillId { get; set; }

        public virtual Skill Skill { get; set; }

        public short SkillVNum { get; set; }

        public byte Slot { get; set; }

        public byte Type { get; set; }

        #endregion
    }
}
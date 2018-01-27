namespace OpenNosCore.Database.Entities
{
    public class Combo
    {
        #region Properties

        public short Animation { get; set; }

        public int ComboId { get; set; }

        public short Effect { get; set; }

        public short Hit { get; set; }

        public virtual Skill Skill { get; set; }

        public short SkillVNum { get; set; }

        #endregion
    }
}
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class RollGeneratedItem
    {
        #region Instantiation

        #endregion

        #region Properties

        [Key]
        public short RollGeneratedItemId { get; set; }
        
        public short OriginalItemDesign { get; set; }

        public virtual Item OriginalItem { get; set; }

        public short OriginalItemVNum { get; set; }

        public short Probability { get; set; }

        public byte ItemGeneratedAmount { get; set; }

        public short ItemGeneratedVNum { get; set; }

        public byte ItemGeneratedUpgrade { get; set; }

        public bool IsRareRandom { get; set; }

        public short MinimumOriginalItemRare { get; set; }

        public short MaximumOriginalItemRare { get; set; }

        public virtual Item ItemGenerated { get; set; }

        public bool IsSuperReward { get; set; }

        #endregion
    }
}
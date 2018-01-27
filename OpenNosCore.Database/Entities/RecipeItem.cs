namespace OpenNosCore.Database.Entities
{
    public class RecipeItem
    {
        #region Properties

        public short Amount { get; set; }

        public virtual Item Item { get; set; }

        public short ItemVNum { get; set; }

        public virtual Recipe Recipe { get; set; }

        public short RecipeId { get; set; }

        public short RecipeItemId { get; set; }

        #endregion
    }
}
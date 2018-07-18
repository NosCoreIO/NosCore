using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Shop
    {
        #region Instantiation

        public Shop()
        {
            ShopItem = new HashSet<ShopItem>();
            ShopSkill = new HashSet<ShopSkill>();
        }

        #endregion

        #region Properties

        public virtual MapNpc MapNpc { get; set; }

        public int MapNpcId { get; set; }

        public byte MenuType { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public int ShopId { get; set; }

        public virtual ICollection<ShopItem> ShopItem { get; set; }

        public virtual ICollection<ShopSkill> ShopSkill { get; set; }

        public byte ShopType { get; set; }

        #endregion
    }
}
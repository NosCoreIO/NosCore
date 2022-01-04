//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.ITEMS_LOADED)]
    public class Item : IStaticEntity
    {
        public Item()
        {
            Drop = new HashSet<Drop>();
            Recipe = new HashSet<Recipe>();
            RollGeneratedItem = new HashSet<RollGeneratedItem>();
            RollGeneratedItem2 = new HashSet<RollGeneratedItem>();
            RecipeItem = new HashSet<RecipeItem>();
            ShopItem = new HashSet<ShopItem>();
            BCards = new HashSet<BCard>();
            ItemInstances = new HashSet<ItemInstance>();
            BattlePassItem = new HashSet<BattlepassItem>();
        }

        public byte BasicUpgrade { get; set; }

        public virtual ICollection<BattlepassItem> BattlePassItem { get; set; }

        public bool Flag1 { get; set; }
        public bool Flag2 { get; set; }
        public bool Flag3 { get; set; }
        public bool Flag4 { get; set; }
        public bool RequireBinding { get; set; }
        public bool Flag6 { get; set; }
        public bool Flag7 { get; set; }
        public bool Flag8 { get; set; }

        public byte CellonLvl { get; set; }

        public byte Class { get; set; }

        public short CloseDefence { get; set; }

        public byte Color { get; set; }

        public short Concentrate { get; set; }

        public byte CriticalLuckRate { get; set; }

        public short CriticalRate { get; set; }

        public short DamageMaximum { get; set; }

        public short DamageMinimum { get; set; }

        public byte DarkElement { get; set; }

        public short DarkResistance { get; set; }

        public short DefenceDodge { get; set; }

        public short DistanceDefence { get; set; }

        public short DistanceDefenceDodge { get; set; }

        public virtual ICollection<Drop> Drop { get; set; }

        public ItemEffectType Effect { get; set; }

        public int EffectValue { get; set; }

        public ElementType Element { get; set; }

        public short ElementRate { get; set; }

        public EquipmentType EquipmentSlot { get; set; }

        public byte FireElement { get; set; }

        public short FireResistance { get; set; }

        public byte Height { get; set; }

        public short HitRate { get; set; }

        public short Hp { get; set; }

        public short HpRegeneration { get; set; }

        public bool IsMinilandActionable { get; set; }

        public bool IsColored { get; set; }

        public bool IsConsumable { get; set; }

        public bool IsDroppable { get; set; }

        public bool IsHeroic { get; set; }

        public bool Flag9 { get; set; }

        public bool IsWarehouse { get; set; }

        public bool IsSoldable { get; set; }

        public bool IsTradable { get; set; }

        public virtual ICollection<BCard> BCards { get; set; }

        public virtual ICollection<ItemInstance> ItemInstances { get; set; }

        public byte ItemSubType { get; set; }

        public ItemType ItemType { get; set; }

        public long ItemValidTime { get; set; }

        public byte LevelJobMinimum { get; set; }

        public byte LevelMinimum { get; set; }

        public byte LightElement { get; set; }

        public short LightResistance { get; set; }

        public short MagicDefence { get; set; }

        public virtual ICollection<RollGeneratedItem> RollGeneratedItem { get; set; }

        public virtual ICollection<RollGeneratedItem> RollGeneratedItem2 { get; set; }

        public byte MaxCellon { get; set; }

        public byte MaxCellonLvl { get; set; }

        public short MaxElementRate { get; set; }

        public byte MaximumAmmo { get; set; }

        public int MinilandObjectPoint { get; set; }

        public short MoreHp { get; set; }

        public short MoreMp { get; set; }

        public short Morph { get; set; }

        public short SecondMorph { get; set; }

        public short Mp { get; set; }

        public short MpRegeneration { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NItem))]
        public string Name { get; set; } = "";

        public long Price { get; set; }

        public short PvpDefence { get; set; }

        public byte PvpStrength { get; set; }

        public virtual ICollection<Recipe> Recipe { get; set; }

        public virtual ICollection<RecipeItem> RecipeItem { get; set; }

        public short ReduceOposantResistance { get; set; }

        public byte ReputationMinimum { get; set; }

        public long ReputPrice { get; set; }

        public ElementType SecondaryElement { get; set; }

        public byte Sex { get; set; }

        public virtual ICollection<ShopItem> ShopItem { get; set; }

        public byte Speed { get; set; }

        public byte SpType { get; set; }

        public NoscorePocketType Type { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short VNum { get; set; }

        public short WaitDelay { get; set; }

        public byte WaterElement { get; set; }

        public short WaterResistance { get; set; }

        public byte Width { get; set; }
    }
}
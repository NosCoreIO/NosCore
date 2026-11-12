//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.RarityPointService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using System.Collections.Generic;

namespace NosCore.GameObject.Tests.Services.ItemGeneration
{
    [TestClass]
    public class RarityPointTests
    {
        private static WearableInstance Create(EquipmentType slot, sbyte rare, byte level, bool heroic = false)
        {
            var items = new List<ItemDto>
            {
                new Item
                {
                    VNum = 1, Type = NoscorePocketType.Equipment, ItemType = slot == EquipmentType.Armor ? ItemType.Armor : ItemType.Weapon,
                    EquipmentSlot = slot, LevelMinimum = level, IsHeroic = heroic
                }
            };
            var provider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                NullLoggerFactory.Instance, TestLocalizers.Log, new RarityPointService());
            return (WearableInstance)provider.Create(1, 1, rare);
        }

        [TestMethod]
        [DataRow((sbyte)7, (byte)50, 110)]
        [DataRow((sbyte)1, (byte)0, 1)]
        public void ARareWeaponSpendsItsPointsOnDamageAndConcentration(sbyte rare, byte level, int points)
        {
            var weapon = Create(EquipmentType.MainWeapon, rare, level);

            Assert.AreEqual(points, weapon.Concentrate + weapon.DamageMinimum);
            Assert.AreEqual(weapon.Concentrate, weapon.HitRate);
            Assert.AreEqual(weapon.DamageMinimum, weapon.DamageMaximum);
        }

        [TestMethod]
        public void AHeroicItemCountsTheRegularLevelsBeforeItsOwn()
        {
            var armour = Create(EquipmentType.Armor, 7, 30, heroic: true);

            Assert.AreEqual(10 * ((95 + 30) / 5 + 1), armour.CloseDefence + armour.DefenceDodge);
        }
    }
}

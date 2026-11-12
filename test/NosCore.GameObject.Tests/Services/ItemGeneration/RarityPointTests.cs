//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.RarityPointService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ItemGeneration
{
    [TestClass]
    public class RarityPointTests
    {
        private static async Task<WearableInstance> CreateAsync(EquipmentType slot, sbyte rare, byte level, bool heroic = false)
        {
            var world = await TestWorld.CreateAsync();
            var items = new List<ItemDto>
            {
                new Item
                {
                    VNum = 1, Type = NoscorePocketType.Equipment, ItemType = slot == EquipmentType.Armor ? ItemType.Armor : ItemType.Weapon,
                    EquipmentSlot = slot, LevelMinimum = level, IsHeroic = heroic
                }
            };
            world.Inject(items);
            world.Inject<IRarityPointService>(new RarityPointService());
            return (WearableInstance)world.Create<GameObject.Services.ItemGenerationService.ItemGenerationService>().Create(1, 1, rare);
        }

        [TestMethod]
        [DataRow((sbyte)7, (byte)50, 110)]
        [DataRow((sbyte)1, (byte)0, 1)]
        public async Task ARareWeaponSpendsItsPointsOnDamageAndConcentration(sbyte rare, byte level, int points)
        {
            var weapon = await CreateAsync(EquipmentType.MainWeapon, rare, level);

            Assert.AreEqual(points, weapon.Concentrate + weapon.DamageMinimum);
            Assert.AreEqual(weapon.Concentrate, weapon.HitRate);
            Assert.AreEqual(weapon.DamageMinimum, weapon.DamageMaximum);
        }

        [TestMethod]
        public async Task AHeroicItemCountsTheRegularLevelsBeforeItsOwn()
        {
            var armour = await CreateAsync(EquipmentType.Armor, 7, 30, heroic: true);

            Assert.AreEqual(10 * ((95 + 30) / 5 + 1), armour.CloseDefence + armour.DefenceDodge);
        }
    }
}

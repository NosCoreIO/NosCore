//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Tests.Services.BazaarService
{
    [TestClass]
    public class BazaarSearchTests
    {
        private const short Sword = 1;
        private const short HeroicBow = 2;
        private const short Hat = 3;
        private const short Ring = 4;
        private const short Potion = 5;
        private const short Snack = 6;
        private const short SpecialistCard = 7;
        private const short SpecialistHolder = 8;
        private const short PetBead = 9;
        private const short AnyClassStick = 10;

        private readonly List<ItemDto> _items = new()
        {
            new Item { VNum = Sword, Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon, Class = 2, LevelMinimum = 25 },
            new Item { VNum = HeroicBow, Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon, Class = 4, LevelMinimum = 90, IsHeroic = true },
            new Item { VNum = Hat, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion, EquipmentSlot = EquipmentType.Hat, LevelMinimum = 1 },
            new Item { VNum = Ring, Type = NoscorePocketType.Equipment, ItemType = ItemType.Jewelery, EquipmentSlot = EquipmentType.Ring, LevelMinimum = 40 },
            new Item { VNum = Potion, Type = NoscorePocketType.Main, ItemType = ItemType.Potion },
            new Item { VNum = Snack, Type = NoscorePocketType.Etc, ItemType = ItemType.Snack },
            new Item { VNum = SpecialistCard, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist, Morph = 10 },
            new Item { VNum = SpecialistHolder, Type = NoscorePocketType.Equipment, ItemType = ItemType.Box, ItemSubType = 2, Flag9 = true },
            new Item { VNum = PetBead, Type = NoscorePocketType.Equipment, ItemType = ItemType.Box, ItemSubType = 0 },
            new Item { VNum = AnyClassStick, Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon, Class = 15, LevelMinimum = 1 },
        };

        private readonly List<BazaarLink> _links = new();
        private GameObject.Services.BazaarService.BazaarService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            List(Sword, rare: 3, upgrade: 5);
            List(HeroicBow);
            List(Hat);
            List(Ring);
            List(Potion);
            List(Snack);
            List(SpecialistHolder, new BoxInstanceDto { ItemVNum = SpecialistHolder, HoldingVNum = SpecialistCard, SpLevel = 45 });
            List(SpecialistHolder, new BoxInstanceDto { ItemVNum = SpecialistHolder });
            List(AnyClassStick);
            List(PetBead, new BoxInstanceDto { ItemVNum = PetBead, HoldingVNum = 333, SpLevel = 12 });

            var registry = new Mock<GameObject.Services.BazaarService.IBazaarRegistry>();
            registry.Setup(s => s.GetAll()).Returns(_links);
            _service = new GameObject.Services.BazaarService.BazaarService(registry.Object, new Mock<IDao<BazaarItemDto, long>>().Object,
                new Mock<IDao<IItemInstanceDto?, Guid>>().Object, new Mock<IClock>().Object, _items);
        }

        private void List(short vnum, ItemInstanceDto? instance = null, sbyte rare = 0, byte upgrade = 0)
        {
            instance ??= new ItemInstanceDto { ItemVNum = vnum };
            instance.Rare = rare;
            instance.Upgrade = upgrade;
            _links.Add(new BazaarLink
            {
                BazaarItem = new BazaarItemDto { BazaarItemId = _links.Count },
                ItemInstance = instance
            });
        }

        private short[] Search(BazaarListType type, byte subType = 0, byte level = 0, byte rare = 0, byte upgrade = 0) =>
            _service.GetBazaar(-1, null, null, type, subType, level, rare, upgrade, null)
                .Select(s => s.ItemInstance!.ItemVNum)
                .ToArray();

        [TestMethod]
        public void NoTypeReturnsEverything()
        {
            Assert.AreEqual(_links.Count, Search(BazaarListType.Default).Length);
        }

        [DataTestMethod]
        [DataRow(BazaarListType.Weapon, new short[] { Sword, HeroicBow, AnyClassStick })]
        [DataRow(BazaarListType.Equipment, new short[] { Hat })]
        [DataRow(BazaarListType.Jewelery, new short[] { Ring })]
        [DataRow(BazaarListType.Main, new short[] { Potion })]
        [DataRow(BazaarListType.Usable, new short[] { Snack })]
        [DataRow(BazaarListType.Specialist, new short[] { SpecialistHolder, SpecialistHolder })]
        [DataRow(BazaarListType.Pet, new short[] { PetBead })]
        [DataRow(BazaarListType.Other, new short[] { PetBead })]
        public void TypeKeepsOnlyItsCategory(BazaarListType type, short[] expected)
        {
            CollectionAssert.AreEquivalent(expected, Search(type));
        }

        [DataTestMethod]
        [DataRow((byte)1, new short[] { AnyClassStick })]
        [DataRow((byte)0, new short[] { Sword, HeroicBow, AnyClassStick })]
        [DataRow((byte)3, new short[] { Sword })]
        [DataRow((byte)11, new short[] { HeroicBow })]
        [DataRow((byte)9, new short[] { HeroicBow })]
        public void WeaponLevelFilterUsesTenLevelBandsAndHeroic(byte level, short[] expected)
        {
            CollectionAssert.AreEquivalent(expected, Search(BazaarListType.Weapon, level: level));
        }

        [DataTestMethod]
        [DataRow((byte)1, new short[] { Sword, AnyClassStick }, DisplayName = "Swordsman")]
        [DataRow((byte)2, new short[] { HeroicBow, AnyClassStick }, DisplayName = "Archer")]
        [DataRow((byte)3, new short[] { AnyClassStick }, DisplayName = "Magician")]
        [DataRow((byte)4, new short[] { AnyClassStick }, DisplayName = "Adventurer")]
        [DataRow((byte)5, new short[0], DisplayName = "Martial artist")]
        public void WeaponClassFilterFollowsTheClientDropDown(byte subType, short[] expected)
        {
            CollectionAssert.AreEquivalent(expected, Search(BazaarListType.Weapon, subType: subType));
        }

        [TestMethod]
        public void RareAndUpgradeFiltersAreOneBased()
        {
            CollectionAssert.AreEquivalent(new[] { Sword }, Search(BazaarListType.Weapon, rare: 4));
            CollectionAssert.AreEquivalent(new[] { Sword }, Search(BazaarListType.Weapon, upgrade: 6));
            Assert.AreEqual(0, Search(BazaarListType.Weapon, rare: 3).Length);
        }

        [TestMethod]
        public void SlotSubTypesPickTheSlot()
        {
            CollectionAssert.AreEquivalent(new[] { Hat }, Search(BazaarListType.Equipment, subType: 1));
            Assert.AreEqual(0, Search(BazaarListType.Equipment, subType: 2).Length);
            CollectionAssert.AreEquivalent(new[] { Ring }, Search(BazaarListType.Jewelery, subType: 2));
            Assert.AreEqual(0, Search(BazaarListType.Jewelery, subType: 1).Length);
        }

        [TestMethod]
        public void MainAndUsableSubTypesPickTheItemType()
        {
            CollectionAssert.AreEquivalent(new[] { Potion }, Search(BazaarListType.Main, subType: 5));
            Assert.AreEqual(0, Search(BazaarListType.Main, subType: 1).Length);
            CollectionAssert.AreEquivalent(new[] { Snack }, Search(BazaarListType.Usable, subType: 2));
            Assert.AreEqual(0, Search(BazaarListType.Usable, subType: 1).Length);
        }

        [TestMethod]
        public void SpecialistSubTypesSplitEmptyHoldersAndMorphs()
        {
            Assert.AreEqual(1, Search(BazaarListType.Specialist, subType: 1).Length);
            Assert.AreEqual(1, Search(BazaarListType.Specialist, subType: 2).Length);
            Assert.AreEqual(0, Search(BazaarListType.Specialist, subType: 3).Length);
            Assert.AreEqual(1, Search(BazaarListType.Specialist, level: 5).Length);
        }

        [TestMethod]
        public void PetLevelFilterReadsTheHeldLevel()
        {
            CollectionAssert.AreEquivalent(new[] { PetBead }, Search(BazaarListType.Pet, level: 2));
            Assert.AreEqual(0, Search(BazaarListType.Pet, level: 1).Length);
            Assert.AreEqual(0, Search(BazaarListType.Pet, subType: 1).Length);
        }

        [TestMethod]
        public void PagingSkipsWholePages()
        {
            var firstPage = _service.GetBazaar(-1, 0, 4, null, null, null, null, null, null);
            var secondPage = _service.GetBazaar(-1, 1, 4, null, null, null, null, null, null);
            var lastPage = _service.GetBazaar(-1, 2, 4, null, null, null, null, null, null);

            CollectionAssert.AreEqual(_links.Take(4).ToList(), firstPage);
            CollectionAssert.AreEqual(_links.Skip(4).Take(4).ToList(), secondPage);
            Assert.AreEqual(2, lastPage.Count);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Algorithm.SpecialistPointService;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.ItemInfoService
{
    [TestClass]
    public class ItemInfoServiceTests
    {
        private readonly List<EquipmentOptionDto> _options = new();
        private readonly List<SkillDto> _skills = new();
        private GameObject.Services.ItemInfoService.ItemInfoService _service;

        [TestInitialize]
        public void Setup()
        {
            var optionDao = new Mock<IDao<EquipmentOptionDto, Guid>>();
            optionDao.Setup(d => d.Where(It.IsAny<Expression<Func<EquipmentOptionDto, bool>>>()))
                .Returns((Expression<Func<EquipmentOptionDto, bool>> predicate) => _options.Where(predicate.Compile()));
            var pointService = new SpecialistPointService();
            _service = new GameObject.Services.ItemInfoService.ItemInfoService(optionDao.Object, _skills,
                new SpExperienceService(), pointService, new SpecialistCardService(pointService));
        }

        private static WearableInstance Wearable(Item item) =>
            new(item, new Mock<ILogger<WearableInstance>>().Object, TestLocalizers.Log);

        private T Info<T>(IItemInstance instance) where T : class =>
            _service.GenerateInfo(instance) as T ?? throw new AssertFailedException($"Expected a {typeof(T).Name}.");

        [TestMethod]
        public void AWeaponShowsItsDamageWithTheInstanceBonusAdded()
        {
            var sword = Wearable(new Item
            {
                VNum = 21, ItemType = ItemType.Weapon, EquipmentSlot = EquipmentType.MainWeapon, Class = 2,
                LevelMinimum = 29, DamageMinimum = 86, DamageMaximum = 104, HitRate = 95, CriticalLuckRate = 6, Price = 12180
            });
            sword.Rare = 3;
            sword.Upgrade = 5;
            sword.DamageMinimum = 37;
            sword.DamageMaximum = 45;
            sword.HitRate = 41;

            PacketAssert.AreEqual(new EInfoWeaponPacket
            {
                Type = EInfoPacketType.WeaponDefault,
                ItemVNum = 21,
                Rare = 3,
                Upgrade = 5,
                LevelMinimum = 29,
                DamageMinimum = 123,
                DamageMaximum = 149,
                HitRate = 136,
                CriticalLuckRate = 6,
                Price = 12180,
                ShellOptions = [],
            }, Info<EInfoWeaponPacket>(sword));
        }

        [TestMethod]
        public void AWeaponShowsItsPityGaugeLast()
        {
            var sword = Wearable(new Item
            {
                VNum = 21, ItemType = ItemType.Weapon, EquipmentSlot = EquipmentType.MainWeapon, Class = 2
            });
            sword.Upgrade = 10;
            sword.PityGauge = 35;

            Assert.AreEqual(35, Info<EInfoWeaponPacket>(sword).PityGauge);
        }

        [TestMethod]
        public void AnArmourShowsItsPityGaugeLast()
        {
            var armour = Wearable(new Item
            {
                VNum = 97, ItemType = ItemType.Armor, EquipmentSlot = EquipmentType.Armor
            });
            armour.Upgrade = 11;
            armour.PityGauge = 420;

            Assert.AreEqual(420, Info<EInfoArmorPacket>(armour).PityGauge);
        }

        [TestMethod]
        public void ABowIsShownAsARangedWeapon()
        {
            var bow = Wearable(new Item
            {
                VNum = 41, ItemType = ItemType.Weapon, EquipmentSlot = EquipmentType.MainWeapon, Class = 4, MaximumAmmo = 100
            });

            Assert.AreEqual(EInfoPacketType.MainWeaponArcher, Info<EInfoWeaponPacket>(bow).Type);
        }

        [TestMethod]
        public void AnArmourShowsItsDefences()
        {
            var armour = Wearable(new Item
            {
                VNum = 97, ItemType = ItemType.Armor, EquipmentSlot = EquipmentType.Armor, LevelMinimum = 27,
                CloseDefence = 58, DistanceDefence = 53, MagicDefence = 36, DefenceDodge = 118, Price = 11340
            });
            armour.Rare = 3;
            armour.Upgrade = 5;
            armour.CloseDefence = 25;
            armour.DistanceDefence = 23;
            armour.MagicDefence = 16;
            armour.BoundCharacterId = 7;

            PacketAssert.AreEqual(new EInfoArmorPacket
            {
                ItemVNum = 97,
                Rare = 3,
                Upgrade = 5,
                LevelMinimum = 27,
                CloseDefence = 83,
                DistanceDefence = 76,
                MagicDefence = 52,
                DefenceDodge = 118,
                Price = 11340,
                BoundCharacterId = 7,
                ShellOptions = [],
            }, Info<EInfoArmorPacket>(armour));
        }

        [TestMethod]
        public void GlovesShowTheirResistances()
        {
            var gloves = Wearable(new Item
            {
                VNum = 412, ItemType = ItemType.Fashion, EquipmentSlot = EquipmentType.Gloves, LevelMinimum = 76,
                CloseDefence = 7, DistanceDefence = 12, MagicDefence = 5, DefenceDodge = 8, FireResistance = 42,
                LightResistance = 42, Price = 37468
            });
            gloves.Upgrade = 5;

            PacketAssert.AreEqual(new EInfoAccessoryPacket
            {
                Type = EInfoPacketType.Hat,
                ItemVNum = 412,
                LevelMinimum = 76,
                CloseDefence = 7,
                DistanceDefence = 12,
                MagicDefence = 5,
                DefenceDodge = 8,
                FireResistance = 42,
                LightResistance = 42,
                Price = 37468,
                Upgrade = 5,
            }, Info<EInfoAccessoryPacket>(gloves));
        }

        [TestMethod]
        public void AFairyShowsItsElementAndRate()
        {
            var fairy = Wearable(new Item
            {
                VNum = 884, ItemType = ItemType.Jewelery, EquipmentSlot = EquipmentType.Fairy, Element = ElementType.Fire, ElementRate = 30
            });
            fairy.ElementRate = 4;

            PacketAssert.AreEqual(new EInfoFairyPacket
            {
                ItemVNum = 884,
                Element = ElementType.Fire,
                ElementRate = 34,
            }, Info<EInfoFairyPacket>(fairy));
        }

        [TestMethod]
        public void ARingListsItsCellonOptions()
        {
            var ring = Wearable(new Item
            {
                VNum = 511, ItemType = ItemType.Jewelery, EquipmentSlot = EquipmentType.Ring, LevelMinimum = 30,
                MaxCellonLvl = 3, MaxCellon = 1, Price = 27300
            });
            _options.Add(new EquipmentOptionDto { WearableInstanceId = ring.Id, Level = 2, Type = 1, Value = 120 });
            _options.Add(new EquipmentOptionDto { WearableInstanceId = Guid.NewGuid(), Level = 1, Type = 3, Value = 9 });

            PacketAssert.AreEqual(new EInfoJewelryPacket
            {
                ItemVNum = 511,
                LevelMinimum = 30,
                MaxCellonLevel = 3,
                MaxCellon = 1,
                CellonOptionCount = 1,
                Price = 27300,
                CellonOptions = [new EInfoCellonOptionSubPacket { Type = 1, Level = 2, Value = 120 }],
            }, Info<EInfoJewelryPacket>(ring));
        }

        [TestMethod]
        public void ARingWithoutCellonsListsNone()
        {
            var ring = Wearable(new Item
            {
                VNum = 511, ItemType = ItemType.Jewelery, EquipmentSlot = EquipmentType.Ring, LevelMinimum = 30,
                MaxCellonLvl = 3, MaxCellon = 1, Price = 27300
            });

            PacketAssert.AreEqual(new EInfoJewelryPacket
            {
                ItemVNum = 511,
                LevelMinimum = 30,
                MaxCellonLevel = 3,
                MaxCellon = 1,
                Price = 27300,
                CellonOptions = [],
            }, Info<EInfoJewelryPacket>(ring));
        }

        [TestMethod]
        public void AnItemWithoutADetailWindowGetsNoAnswer()
        {
            var costume = Wearable(new Item { VNum = 1, ItemType = ItemType.Fashion, EquipmentSlot = EquipmentType.CostumeSuit });

            Assert.IsNull(_service.GenerateInfo(costume));
        }

        [TestMethod]
        public void ASpecialistCardShowsItsSkillsLevelAndSkillPoints()
        {
            for (short vnum = 822; vnum <= 833; vnum++)
            {
                _skills.Add(new SkillDto { SkillVNum = vnum, Class = 34, UpgradeType = 3, LevelMinimum = vnum == 833 ? (byte)50 : (byte)1 });
            }
            var card = new SpecialistInstance(new Item
            {
                VNum = 902, ItemType = ItemType.Specialist, EquipmentSlot = EquipmentType.Sp, Morph = 3,
                LevelJobMinimum = 35, ReputationMinimum = 7, SpType = 3, FireResistance = 3, WaterResistance = 11, LightResistance = 8
            })
            {
                SpLevel = 40,
                Upgrade = 3,
                Xp = 1234,
                SlDamage = 30,
            };

            PacketAssert.AreEqual(new SlInfoPacket
            {
                InventoryType = PocketType.Equipment,
                ItemVnum = 902,
                ItemMorph = 3,
                SpLevel = 40,
                LevelJobMinimum = 35,
                ReputationMinimum = 7,
                SpType = 3,
                FireResistance = 3,
                WaterResistance = 11,
                LightResistance = 8,
                Xp = 1234,
                SpXpData = 554000,
                Skill = "823.824.825.826.827.828.829.830.831.832",
                TransportId = card.TransportId,
                FreeSpPoints = 45,
                SlHit = 19,
                Upgrade = 3,
            }, Info<SlInfoPacket>(card));
        }

        [TestMethod]
        public void ASpecialistCardWithoutSkillsSendsMinusOne()
        {
            var card = new SpecialistInstance(new Item { VNum = 900, ItemType = ItemType.Specialist, EquipmentSlot = EquipmentType.Sp, Morph = 1 })
            {
                SpLevel = 1,
            };

            Assert.AreEqual("-1", Info<SlInfoPacket>(card).Skill);
        }

        [TestMethod]
        public void AWeaponListsItsShellOptions()
        {
            var sword = Wearable(new Item
            {
                VNum = 299, ItemType = ItemType.Weapon, EquipmentSlot = EquipmentType.MainWeapon, LevelMinimum = 70, Price = 100
            });
            sword.ShellRarity = 5;
            sword.BoundCharacterId = 1;
            _options.Add(new EquipmentOptionDto { WearableInstanceId = sword.Id, Level = 2, Type = 15, Value = 12 });
            _options.Add(new EquipmentOptionDto { WearableInstanceId = sword.Id, Level = 1, Type = 1, Value = 50 });

            PacketAssert.AreEqual(new EInfoWeaponPacket
            {
                ItemVNum = 299,
                LevelMinimum = 70,
                Price = 100,
                ShellRarity = 5,
                BoundCharacterId = 1,
                ShellOptionCount = 2,
                ShellOptions =
                [
                    new EInfoShellOptionSubPacket { Level = 1, Type = 1, Value = 50 },
                    new EInfoShellOptionSubPacket { Level = 2, Type = 15, Value = 12 },
                ],
            }, Info<EInfoWeaponPacket>(sword));
        }

        [TestMethod]
        public void AShellShowsItsLevelRarityAndOptions()
        {
            var shell = Wearable(new Item
            {
                VNum = 565, ItemType = ItemType.Shell, EquipmentSlot = EquipmentType.MainWeapon, Price = 200
            });
            shell.Upgrade = 60;
            shell.Rare = 5;
            _options.Add(new EquipmentOptionDto { WearableInstanceId = shell.Id, Level = 1, Type = 1, Value = 50 });

            PacketAssert.AreEqual(new EInfoShellPacket
            {
                ItemVNum = 565,
                Level = 60,
                Rare = 5,
                Price = 200,
                ShellOptionCount = 1,
                ShellOptions = [new EInfoShellOptionSubPacket { Level = 1, Type = 1, Value = 50 }],
            }, Info<EInfoShellPacket>(shell));
        }
    }
}

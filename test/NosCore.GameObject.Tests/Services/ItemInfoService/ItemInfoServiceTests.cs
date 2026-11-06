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
using NosCore.Packets;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.ItemInfoService
{
    [TestClass]
    public class ItemInfoServiceTests
    {
        private static readonly Serializer Serializer = new(typeof(IPacket).Assembly.GetTypes()
            .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract)
            .ToList());

        private readonly List<EquipmentOptionDto> _options = new();
        private readonly List<SkillDto> _skills = new();
        private GameObject.Services.ItemInfoService.ItemInfoService _service = null!;

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
            new(item, new Mock<ILogger<WearableInstance>>().Object, TestHelpers.Instance.LogLanguageLocalizer);

        private string Info(IItemInstance instance) =>
            Serializer.Serialize(new[] { _service.GenerateInfo(instance)! }).TrimEnd();

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

            Assert.AreEqual("e_info 0 21 3 5 0 29 123 149 136 6 0 0 0 12180 -1 0 0 0 0", Info(sword));
        }

        [TestMethod]
        public void ABowIsShownAsARangedWeapon()
        {
            var bow = Wearable(new Item
            {
                VNum = 41, ItemType = ItemType.Weapon, EquipmentSlot = EquipmentType.MainWeapon, Class = 4, MaximumAmmo = 100
            });

            StringAssert.StartsWith(Info(bow), "e_info 1 41 ");
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

            Assert.AreEqual("e_info 2 97 3 5 0 27 83 76 52 118 11340 -1 0 7 0 0", Info(armour));
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

            Assert.AreEqual("e_info 3 412 76 7 12 5 8 42 0 42 0 37468 5 0 -1", Info(gloves));
        }

        [TestMethod]
        public void AFairyShowsItsElementAndRate()
        {
            var fairy = Wearable(new Item
            {
                VNum = 884, ItemType = ItemType.Jewelery, EquipmentSlot = EquipmentType.Fairy, Element = ElementType.Fire, ElementRate = 30
            });
            fairy.ElementRate = 4;

            Assert.AreEqual("e_info 4 884 1 34 0 0 0 0 0", Info(fairy));
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

            Assert.AreEqual("e_info 4 511 30 3 1 1 27300 1 2 120", Info(ring));
        }

        [TestMethod]
        public void ARingWithoutCellonsMatchesTheClientLayout()
        {
            var ring = Wearable(new Item
            {
                VNum = 511, ItemType = ItemType.Jewelery, EquipmentSlot = EquipmentType.Ring, LevelMinimum = 30,
                MaxCellonLvl = 3, MaxCellon = 1, Price = 27300
            });

            Assert.AreEqual("e_info 4 511 30 3 1 0 27300", Info(ring));
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

            var info = Info(card);

            StringAssert.StartsWith(info,
                $"slinfo 0 902 3 40 35 7 0 0 0 0 0 0 0 3 3 11 8 0 1234 554000 823.824.825.826.827.828.829.830.831.832 {card.TransportId} 19 45 0 0 0 3 0 0 0 ");
        }

        [TestMethod]
        public void ASpecialistCardWithoutSkillsSendsMinusOne()
        {
            var card = new SpecialistInstance(new Item { VNum = 900, ItemType = ItemType.Specialist, EquipmentSlot = EquipmentType.Sp, Morph = 1 })
            {
                SpLevel = 1,
            };

            StringAssert.Contains(Info(card), " 15000 -1 ");
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

            Assert.AreEqual("e_info 0 299 0 0 0 70 0 0 0 0 0 0 0 100 -1 5 1 2 1.1.50 2.15.12 0", Info(sword));
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

            Assert.AreEqual("e_info 9 565 60 5 200 1 1.1.50", Info(shell));
        }
    }
}

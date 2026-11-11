//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MpService;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.SkillService;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class StarterGearTests
    {
        private TestWorld World;
        private const short Weapon = 1;
        private const short Fairy = 2;
        private const short Seed = 1012;
        private const short Costume = 924;

        private readonly List<IItemInstanceDto> _savedItems = new();
        private readonly List<InventoryItemInstanceDto> _savedSlots = new();
        private IOptions<WorldConfiguration> _configuration;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var configuration = World.WorldConfiguration.Value;
            configuration.BackpackSize = 48;
            configuration.BasicEquipments = new Dictionary<string, Dictionary<StarterOrigin, List<BasicEquipment>>>
            {
                [nameof(CharacterClassType.Adventurer)] = new()
                {
                    [StarterOrigin.CreateAndUpgrade] = new()
                    {
                        new BasicEquipment { VNum = Weapon, Amount = 1, Rare = 2, Upgrade = 3, NoscorePocketType = NoscorePocketType.Equipment },
                        new BasicEquipment { VNum = Seed, Amount = 10, NoscorePocketType = NoscorePocketType.Main }
                    }
                },
                [nameof(CharacterClassType.Swordsman)] = new()
                {
                    [StarterOrigin.Create56] = new() { new BasicEquipment { VNum = Costume, Amount = 1, NoscorePocketType = NoscorePocketType.Equipment } },
                    [StarterOrigin.Create80] = new() { new BasicEquipment { VNum = Fairy, Amount = 1, NoscorePocketType = NoscorePocketType.Equipment } },
                    [StarterOrigin.CreateAndUpgrade] = new() { new BasicEquipment { VNum = Fairy, Amount = 1, NoscorePocketType = NoscorePocketType.Equipment } }
                }
            };
            _configuration = World.WorldConfiguration;
            _session = await World.CreateSessionAsync();
        }

        private async Task CreateAsync(CharacterClassType @class, bool allClassAvailable = false)
        {
            _configuration.Value.AllClassAvailableOnCreate = allClassAvailable;
            var itemInstances = new Mock<IDao<IItemInstanceDto?, Guid>>();
            itemInstances.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<IItemInstanceDto?>>()))
                .Callback<IEnumerable<IItemInstanceDto?>>(items => _savedItems.AddRange(items.OfType<IItemInstanceDto>()))
                .ReturnsAsync(true);
            var slots = new Mock<IDao<InventoryItemInstanceDto, Guid>>();
            slots.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<InventoryItemInstanceDto>>()))
                .Callback<IEnumerable<InventoryItemInstanceDto>>(items => _savedSlots.AddRange(items))
                .ReturnsAsync(true);

            var handler = new CharNewPacketHandler(World.CharacterDao, World.GenerateItemProvider(),
                new Mock<IDao<QuicklistEntryDto, Guid>>().Object, itemInstances.Object, slots.Object, new HpService(), new MpService(),
                _configuration, new Mock<IDao<CharacterSkillDto, Guid>>().Object, World.ItemList, NullLoggerFactory.Instance);
            _session.ClearPlayerEntity();
            await handler.ExecuteAsync(new CharNewPacket { Name = "Starter", Slot = 2, TargetClass = @class }, _session);
        }

        [TestMethod]
        public async Task AnAdventurerStartsWithTheConfiguredPack()
        {
            await CreateAsync(CharacterClassType.Adventurer);

            var weapon = _savedItems.Single(s => s.ItemVNum == Weapon);
            Assert.AreEqual((sbyte)2, weapon.Rare);
            Assert.AreEqual((byte)3, weapon.Upgrade);
            Assert.AreEqual((short)10, _savedItems.Single(s => s.ItemVNum == Seed).Amount);
            Assert.AreEqual(2, _savedItems.Count);

            Assert.AreEqual(NoscorePocketType.Equipment, _savedSlots.Single(s => s.ItemInstanceId == weapon.Id).Type);
            Assert.AreEqual(NoscorePocketType.Main, _savedSlots.Single(s => s.ItemInstanceId != weapon.Id).Type);
        }

        [TestMethod]
        public async Task AClassCreatedAtLevel56GetsTheLevel56Pack()
        {
            await CreateAsync(CharacterClassType.Swordsman);

            CollectionAssert.AreEqual(new[] { Costume }, _savedItems.Select(s => s.ItemVNum).ToArray());
        }

        [TestMethod]
        public async Task AClassCreatedAtLevel80GetsTheLevel80Pack()
        {
            await CreateAsync(CharacterClassType.Swordsman, allClassAvailable: true);

            CollectionAssert.AreEqual(new[] { Fairy }, _savedItems.Select(s => s.ItemVNum).ToArray());
        }

        [TestMethod]
        public async Task AClassWithoutAPackStartsEmpty()
        {
            await CreateAsync(CharacterClassType.Archer);

            Assert.AreEqual(0, _savedItems.Count);
        }

        [TestMethod]
        public async Task ChangingClassAddsTheNewClassPack()
        {
            _session.Character.MapInstance = World.GetBaseMap(1);

            await _session.ChangeClassAsync(CharacterClassType.Swordsman, _configuration, new ExperienceService(),
                new JobExperienceService(), new HeroExperienceService(), World.GenerateItemProvider(),
                new Mock<ISkillService>().Object);

            Assert.AreEqual(CharacterClassType.Swordsman, _session.Character.Class);
            Assert.IsTrue(_session.Character.InventoryService.Values.Any(s => s.ItemInstance.ItemVNum == Fairy));
        }

        [TestMethod]
        public async Task ChangingClassWithEquipmentWornAddsNothing()
        {
            _session.Character.MapInstance = World.GetBaseMap(1);
            var worn = _session.Character.InventoryService.AddItemToPocket(
                GameObject.Services.InventoryService.InventoryItemInstance.Create(World.GenerateItemProvider().Create(Weapon, 1), _session.Character.CharacterId),
                NoscorePocketType.Equipment).NotNull().First();
            Assert.IsNotNull(_session.Character.InventoryService.MoveInPocket(worn.Slot, worn.Type, NoscorePocketType.Wear, 0, false));

            await _session.ChangeClassAsync(CharacterClassType.Swordsman, _configuration, new ExperienceService(),
                new JobExperienceService(), new HeroExperienceService(), World.GenerateItemProvider(),
                new Mock<ISkillService>().Object);

            Assert.AreNotEqual(CharacterClassType.Swordsman, _session.Character.Class);
            Assert.IsFalse(_session.Character.InventoryService.Values.Any(s => s.ItemInstance.ItemVNum == Fairy));
        }
    }
}

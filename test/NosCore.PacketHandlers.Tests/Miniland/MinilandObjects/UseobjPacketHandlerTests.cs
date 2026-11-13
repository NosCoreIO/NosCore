//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Miniland.MinilandObjects;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.Warehouse;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using NosCore.GameObject.Services.UpgradeService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UseObjPacket = NosCore.Packets.ClientPackets.Miniland.UseObjPacket;

namespace NosCore.PacketHandlers.Tests.Miniland.MinilandObjects
{
    [TestClass]
    public class UseobjPacketHandlerTests
    {
        private TestWorld World;
        private UseobjPacketHandler _useobjPacketHandler;
        private ClientSession _session;
        private IMinilandService _minilandProvider;
        private InventoryItemInstance _minilandObject;
        private Mock<IWarehouseHub> _warehouseHubMock;
        private Mock<IDao<IItemInstanceDto?, Guid>> _itemInstanceDaoMock;
        private ItemGenerationService _itemProvider;

        private List<ItemDto> MinilandItems => new()
        {
            new Item { Type = NoscorePocketType.Miniland, VNum = 3000, ItemType = ItemType.Minigame, MinilandObjectPoint = 100, IsWarehouse = false, EquipmentSlot = EquipmentType.Amulet, MinigameType = MinigameType.Sawmill },
            new Item { Type = NoscorePocketType.Miniland, VNum = 3001, ItemType = ItemType.Minigame, MinilandObjectPoint = 50, IsWarehouse = true }
        };

        private static MinigameRewardService MinigameRewards => new(
            new[] { 999, 4999, 9999, 13999, 17999, 1000000 }
                .Select((max, level) => new MinigameScoreDto
                    { MinigameScoreId = (short)(10 + level), MinigameType = MinigameType.Sawmill, Level = (byte)level, MaximumScore = max })
                .ToList(),
            [new MinigameRewardDto { MinigameRewardId = 0, MinigameVNum = 3000, Level = 0, ItemVNum = 1269, Amount = 1 }],
            new RandomNumberSource());

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            await World.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto()
            {
                OwnerId = _session.Character.CharacterId,
            });
            await World.SeedMinilandEntrancesAsync();
            _minilandProvider = new MinilandService(World.MapInstanceAccessorService,
                World.FriendHub.Object,
                new List<MapDto> {new Map
                {
                    MapId = 20001,
                    NameI18NKey = "miniland",
                    Data = new byte[] {}
                }},
                World.MinilandDao,
                World.MinilandObjectDao, World.PortalDao, World.Create<MinilandRegistry>());
            await _minilandProvider.InitializeAsync(_session.Character, World.MapInstanceGeneratorService);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            var mapInstance = World.GetMap(miniland.MapInstanceId);
            _session.Character.MapInstance = mapInstance;
            _warehouseHubMock = new Mock<IWarehouseHub>();
            _itemInstanceDaoMock = new Mock<IDao<IItemInstanceDto?, Guid>>();
            _itemProvider = new ItemGenerationService(
                MinilandItems,
                NullLoggerFactory.Instance,
                World.LogLanguageLocalizer, new RarityPointService());
            _useobjPacketHandler = new UseobjPacketHandler(_minilandProvider, _warehouseHubMock.Object, _itemInstanceDaoMock.Object, _itemProvider,
                MinigameRewards);
        }

        [TestMethod]
        public async Task UseObjectWithoutObjectShouldFail()
        {
            await new Spec("Use object without object should fail")
                .WhenAsync(UsingNonExistentObject)
                .Then(NothingShouldHappen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheInfoShowsTheScoreBandsOfItsMinigame()
        {
            await new Spec("The info shows the score bands of its minigame")
                .Given(MinigameObjectExistsAndPlaced)
                .WhenAsync(UsingMinigameObject)
                .Then(TheScoreBandsShouldEndAt_, new long[] { 999, 4999, 9999, 13999, 17999, 1000000 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UseMinigameObjectShouldShowInfo()
        {
            await new Spec("Use minigame object should show info")
                .Given(MinigameObjectExistsAndPlaced)
                .WhenAsync(UsingMinigameObject)
                .Then(ShouldReceiveMinigameInfoPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UseWarehouseObjectShouldShowWarehouse()
        {
            await new Spec("Use warehouse object should show warehouse")
                .Given(WarehouseObjectExistsAndPlaced)
                .And(WarehouseHubReturnsEmptyList)
                .WhenAsync(UsingWarehouseObject)
                .Then(ShouldReceiveWarehousePacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UseWarehouseObjectShouldShowStoredItemsAtTheirSlot()
        {
            await new Spec("Use warehouse object should show stored items at their slot")
                .Given(WarehouseObjectExistsAndPlaced)
                .And(WarehouseHubReturnsAnItemAtSlot_, (short)7)
                .WhenAsync(UsingWarehouseObject)
                .Then(TheWarehousePacketShouldListAnItemAtSlot_, (short)7)
                .ExecuteAsync();
        }

        private void MinigameObjectExistsAndPlaced()
        {
            var item = _itemProvider.Create(3000, 1);
            item.DurabilityPoint = 1000;
            _minilandObject = InventoryItemInstance.Create(item, _session.Character.CharacterId);
            _minilandObject.Type = NoscorePocketType.Miniland;
            _minilandObject.Slot = 0;
            _session.Character.InventoryService[_minilandObject.Id] = _minilandObject;

            var mapObject = new MapDesignObject
            {
                MinilandObjectId = Guid.NewGuid(),
                MapX = 5,
                MapY = 5,
                Slot = 0
            };
            _minilandProvider.AddMinilandObject(mapObject, _session.Character.CharacterId, _minilandObject);
        }

        private void WarehouseObjectExistsAndPlaced()
        {
            var item = _itemProvider.Create(3001, 1);
            _minilandObject = InventoryItemInstance.Create(item, _session.Character.CharacterId);
            _minilandObject.Type = NoscorePocketType.Miniland;
            _minilandObject.Slot = 0;
            _session.Character.InventoryService[_minilandObject.Id] = _minilandObject;

            var mapObject = new MapDesignObject
            {
                MinilandObjectId = Guid.NewGuid(),
                MapX = 5,
                MapY = 5,
                Slot = 0
            };
            _minilandProvider.AddMinilandObject(mapObject, _session.Character.CharacterId, _minilandObject);
        }

        private void WarehouseHubReturnsEmptyList()
        {
            var characterId = _session.Character.CharacterId;
            _warehouseHubMock.Setup(x => x.GetWarehouseItems(null, characterId, WarehouseType.Warehouse, null))
                .ReturnsAsync(new List<WarehouseLink>());
        }

        private void WarehouseHubReturnsAnItemAtSlot_(short slot)
        {
            var stored = _itemProvider.Create(3000, 1);
            var characterId = _session.Character.CharacterId;
            _warehouseHubMock.Setup(x => x.GetWarehouseItems(null, characterId, WarehouseType.Warehouse, null))
                .ReturnsAsync(new List<WarehouseLink>
                {
                    new() { WarehouseItemId = Guid.NewGuid(), Slot = slot, ItemInstance = new ItemInstanceDto { Id = stored.Id } }
                });
            _itemInstanceDaoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(stored);
        }

        private async Task UsingNonExistentObject()
        {
            var useobjPacket = new UseObjPacket(_session.Character.Name)
            {
                Slot = 99
            };
            await _useobjPacketHandler.ExecuteAsync(useobjPacket, _session);
        }

        private async Task UsingMinigameObject()
        {
            var useobjPacket = new UseObjPacket(_session.Character.Name)
            {
                Slot = 0
            };
            await _useobjPacketHandler.ExecuteAsync(useobjPacket, _session);
        }

        private async Task UsingWarehouseObject()
        {
            var useobjPacket = new UseObjPacket(_session.Character.Name)
            {
                Slot = 0
            };
            await _useobjPacketHandler.ExecuteAsync(useobjPacket, _session);
        }

        private void NothingShouldHappen()
        {
            Assert.AreEqual(0, _session.LastPackets.Count);
        }

        private void TheScoreBandsShouldEndAt_(long[] expected)
        {
            var bands = _session.LastPackets.OfType<MloInfoPacket>().Single().MinigamePoints.NotNull().Select(b => b.NotNull()).ToList();
            CollectionAssert.AreEqual(expected, bands.Select(b => b.MaximumPoints).ToArray());
            CollectionAssert.AreEqual(new[] { 0L }.Concat(expected.SkipLast(1).Select(m => m + 1)).ToArray(),
                bands.Select(b => b.MinimumPoints).ToArray());
        }

        private void ShouldReceiveMinigameInfoPacket()
        {
            var lastPacket = (MloInfoPacket?)_session.LastPackets.FirstOrDefault(s => s is MloInfoPacket);
            Assert.IsNotNull(lastPacket);
        }

        private void TheWarehousePacketShouldListAnItemAtSlot_(short slot)
        {
            var stash = _session.LastPackets.OfType<StashAllPacket>().Single();
            Assert.AreEqual(slot, stash.Items.NotNull().Single().Slot);
        }

        private void ShouldReceiveWarehousePacket()
        {
            var lastPacket = (StashAllPacket?)_session.LastPackets.FirstOrDefault(s => s is StashAllPacket);
            Assert.IsNotNull(lastPacket);
        }
    }
}

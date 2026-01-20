//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Miniland.MinilandObjects;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.Warehouse;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UseObjPacket = NosCore.Packets.ServerPackets.Miniland.UseObjPacket;

namespace NosCore.PacketHandlers.Tests.Miniland.MinilandObjects
{
    [TestClass]
    public class UseobjPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private UseobjPacketHandler _useobjPacketHandler = null!;
        private ClientSession _session = null!;
        private IMinilandService _minilandProvider = null!;
        private InventoryItemInstance _minilandObject = null!;
        private Mock<IWarehouseHub> _warehouseHubMock = null!;
        private Mock<IDao<IItemInstanceDto?, Guid>> _itemInstanceDaoMock = null!;
        private ItemGenerationService _itemProvider = null!;

        private List<ItemDto> MinilandItems => new()
        {
            new Item { Type = NoscorePocketType.Miniland, VNum = 3000, ItemType = ItemType.Minigame, MinilandObjectPoint = 100, IsWarehouse = false, EquipmentSlot = EquipmentType.Amulet },
            new Item { Type = NoscorePocketType.Miniland, VNum = 3001, ItemType = ItemType.Minigame, MinilandObjectPoint = 50, IsWarehouse = true }
        };

        [TestInitialize]
        public async Task SetupAsync()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc());
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            await TestHelpers.Instance.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto()
            {
                OwnerId = _session.Character.CharacterId,
            });
            _minilandProvider = new MinilandService(TestHelpers.Instance.MapInstanceAccessorService,
                TestHelpers.Instance.FriendHttpClient.Object,
                new List<MapDto> {new Map
                {
                    MapId = 20001,
                    NameI18NKey = "miniland",
                    Data = new byte[] {}
                }},
                TestHelpers.Instance.MinilandDao,
                TestHelpers.Instance.MinilandObjectDao, new MinilandRegistry());
            await _minilandProvider.InitializeAsync(_session.Character, TestHelpers.Instance.MapInstanceGeneratorService);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            var mapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetMapInstance(miniland.MapInstanceId)!;
            _session.Character.MapInstance = mapInstance;
            _warehouseHubMock = new Mock<IWarehouseHub>();
            _itemInstanceDaoMock = new Mock<IDao<IItemInstanceDto?, Guid>>();
            _itemProvider = new ItemGenerationService(
                MinilandItems,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()),
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
            _useobjPacketHandler = new UseobjPacketHandler(_minilandProvider, _warehouseHubMock.Object, _itemInstanceDaoMock.Object, _itemProvider);
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
            _warehouseHubMock.Setup(x => x.GetWarehouseItems(null, _session.Character.CharacterId, WarehouseType.Warehouse, null))
                .ReturnsAsync(new List<WarehouseLink>());
        }

        private async Task UsingNonExistentObject()
        {
            var useobjPacket = new UseObjPacket
            {
                ObjectId = 99
            };
            await _useobjPacketHandler.ExecuteAsync(useobjPacket, _session);
        }

        private async Task UsingMinigameObject()
        {
            var useobjPacket = new UseObjPacket
            {
                ObjectId = 0
            };
            await _useobjPacketHandler.ExecuteAsync(useobjPacket, _session);
        }

        private async Task UsingWarehouseObject()
        {
            var useobjPacket = new UseObjPacket
            {
                ObjectId = 0
            };
            await _useobjPacketHandler.ExecuteAsync(useobjPacket, _session);
        }

        private void NothingShouldHappen()
        {
            Assert.AreEqual(0, _session.LastPackets.Count);
        }

        private void ShouldReceiveMinigameInfoPacket()
        {
            var lastPacket = (MloInfoPacket?)_session.LastPackets.FirstOrDefault(s => s is MloInfoPacket);
            Assert.IsNotNull(lastPacket);
        }

        private void ShouldReceiveWarehousePacket()
        {
            var lastPacket = (StashAllPacket?)_session.LastPackets.FirstOrDefault(s => s is StashAllPacket);
            Assert.IsNotNull(lastPacket);
        }
    }
}

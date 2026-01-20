//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Miniland.MinilandObjects;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Miniland.MinilandObjects
{
    [TestClass]
    public class MgPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private MgPacketHandler _mgPacketHandler = null!;
        private ClientSession _session = null!;
        private IMinilandService _minilandProvider = null!;
        private InventoryItemInstance _minilandObject = null!;
        private ItemGenerationService _itemProvider = null!;

        private List<ItemDto> MinilandItems => new()
        {
            new Item { Type = NoscorePocketType.Miniland, VNum = 3000, ItemType = ItemType.Minigame, MinilandObjectPoint = 100, IsWarehouse = false, EquipmentSlot = EquipmentType.Amulet },
            new Item { Type = NoscorePocketType.Miniland, VNum = 3001, ItemType = ItemType.Minigame, MinilandObjectPoint = 100, IsWarehouse = true }
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
            _itemProvider = new ItemGenerationService(
                MinilandItems,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()),
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
            _mgPacketHandler = new MgPacketHandler(_minilandProvider, _itemProvider);
        }

        [TestMethod]
        public async Task PlayMinigameWithoutObjectShouldFail()
        {
            await new Spec("Play minigame without object should fail")
                .WhenAsync(PlayingMinigameWithoutObject)
                .Then(NothingShouldHappen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayMinigameWithWarehouseShouldFail()
        {
            await new Spec("Play minigame with warehouse should fail")
                .Given(WarehouseObjectExistsAndPlaced)
                .WhenAsync(PlayingMinigameOnWarehouse)
                .Then(NothingShouldHappen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayMinigameWithZeroDurabilityShouldFail()
        {
            await new Spec("Play minigame with zero durability should fail")
                .Given(MinigameObjectExistsAndPlacedWithZeroDurability)
                .WhenAsync(PlayingMinigame)
                .Then(ShouldReceiveRestoreDurabilityMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayMinigameWithZeroMinilandPointsShouldAskConfirmation()
        {
            await new Spec("Play minigame with zero miniland points should ask confirmation")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasZeroPoints)
                .WhenAsync(PlayingMinigame)
                .Then(ShouldReceiveConfirmationQuestion)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShowManagementShouldSucceed()
        {
            await new Spec("Show management should succeed")
                .Given(MinigameObjectExistsAndPlaced)
                .WhenAsync(ShowingManagement)
                .Then(ShouldReceiveManagementPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShowGiftsShouldSucceed()
        {
            await new Spec("Show gifts should succeed")
                .Given(MinigameObjectExistsAndPlaced)
                .WhenAsync(ShowingGifts)
                .Then(ShouldReceiveGiftsPacket)
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

        private void MinigameObjectExistsAndPlacedWithZeroDurability()
        {
            var item = _itemProvider.Create(3000, 1);
            item.DurabilityPoint = 0;
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

        private void MinilandHasZeroPoints()
        {
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            miniland.MinilandPoint = 0;
        }

        private async Task PlayingMinigameWithoutObject()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = 1,
                Id = 99,
                MinigameVNum = 3000
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private async Task PlayingMinigameOnWarehouse()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = 1,
                Id = 0,
                MinigameVNum = 3001
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private async Task PlayingMinigame()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = 1,
                Id = 0,
                MinigameVNum = 3000
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private async Task ShowingManagement()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = 5,
                Id = 0,
                MinigameVNum = 3000
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private async Task ShowingGifts()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = 7,
                Id = 0,
                MinigameVNum = 3000
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private void NothingShouldHappen()
        {
            Assert.AreEqual(0, _session.LastPackets.Count);
        }

        private void ShouldReceiveRestoreDurabilityMessage()
        {
            var lastPacket = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsNotNull(lastPacket);
            Assert.AreEqual(Game18NConstString.NeedToRestoreDurability, lastPacket.Message);
        }

        private void ShouldReceiveConfirmationQuestion()
        {
            var lastPacket = (QnaiPacket?)_session.LastPackets.FirstOrDefault(s => s is QnaiPacket);
            Assert.IsNotNull(lastPacket);
            Assert.AreEqual(Game18NConstString.NotEnoughProductionPointsAskStart, lastPacket.Question);
        }

        private void ShouldReceiveManagementPacket()
        {
            var lastPacket = (MloMgPacket?)_session.LastPackets.FirstOrDefault(s => s is MloMgPacket);
            Assert.IsNotNull(lastPacket);
        }

        private void ShouldReceiveGiftsPacket()
        {
            var lastPacket = (MloPmgPacket?)_session.LastPackets.FirstOrDefault(s => s is MloPmgPacket);
            Assert.IsNotNull(lastPacket);
        }
    }
}

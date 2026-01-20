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
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Miniland
{
    [TestClass]
    public class AddobjPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private AddobjPacketHandler _addobjPacketHandler = null!;
        private ClientSession _session = null!;
        private IMinilandService _minilandProvider = null!;
        private InventoryItemInstance _minilandObject = null!;
        private ItemGenerationService _itemProvider = null!;

        private List<ItemDto> MinilandItems => new()
        {
            new Item { Type = NoscorePocketType.Miniland, VNum = 3000, ItemType = ItemType.Minigame, MinilandObjectPoint = 100 }
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
            _addobjPacketHandler = new AddobjPacketHandler(_minilandProvider);
            _itemProvider = new ItemGenerationService(
                MinilandItems,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()),
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task AddObjectWithoutItemShouldFail()
        {
            await new Spec("Add object without item should fail")
                .Given(MinilandIsLocked)
                .WhenAsync(AddingNonExistentObject)
                .Then(NoObjectShouldBeAdded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddObjectWhenMinilandNotLockedShouldFail()
        {
            await new Spec("Add object when miniland not locked should fail")
                .Given(MinilandObjectExists)
                .And(MinilandIsOpen)
                .WhenAsync(AddingMinilandObject)
                .Then(ShouldReceiveInstallationOnlyLockModeMessage)
                .And(NoObjectShouldBeAdded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddObjectWhenLockedShouldSucceed()
        {
            await new Spec("Add object when locked should succeed")
                .Given(MinilandObjectExists)
                .And(MinilandIsLocked)
                .WhenAsync(AddingMinilandObject)
                .Then(ObjectShouldBeAdded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddDuplicateObjectShouldFail()
        {
            await new Spec("Add duplicate object should fail")
                .Given(MinilandObjectExists)
                .And(MinilandIsLocked)
                .And(ObjectAlreadyPlaced)
                .WhenAsync(AddingMinilandObject)
                .Then(ShouldReceiveAlreadyHaveObjectMessage)
                .ExecuteAsync();
        }

        private void MinilandIsLocked()
        {
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            miniland.State = MinilandState.Lock;
        }

        private void MinilandIsOpen()
        {
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            miniland.State = MinilandState.Open;
        }

        private void MinilandObjectExists()
        {
            var item = _itemProvider.Create(3000, 1);
            _minilandObject = InventoryItemInstance.Create(item, _session.Character.CharacterId);
            _minilandObject.Type = NoscorePocketType.Miniland;
            _minilandObject.Slot = 0;
            _session.Character.InventoryService[_minilandObject.Id] = _minilandObject;
        }

        private void ObjectAlreadyPlaced()
        {
            var mapObject = new MapDesignObject
            {
                MinilandObjectId = Guid.NewGuid(),
                MapX = 5,
                MapY = 5
            };
            _minilandProvider.AddMinilandObject(mapObject, _session.Character.CharacterId, _minilandObject);
        }

        private async Task AddingNonExistentObject()
        {
            var addobjPacket = new AddobjPacket
            {
                Slot = 99,
                PositionX = 5,
                PositionY = 5
            };
            await _addobjPacketHandler.ExecuteAsync(addobjPacket, _session);
        }

        private async Task AddingMinilandObject()
        {
            var addobjPacket = new AddobjPacket
            {
                Slot = 0,
                PositionX = 5,
                PositionY = 5
            };
            await _addobjPacketHandler.ExecuteAsync(addobjPacket, _session);
        }

        private void NoObjectShouldBeAdded()
        {
            Assert.AreEqual(0, _session.Character.MapInstance.MapDesignObjects.Count);
        }

        private void ObjectShouldBeAdded()
        {
            Assert.AreEqual(1, _session.Character.MapInstance.MapDesignObjects.Count);
        }

        private void ShouldReceiveInstallationOnlyLockModeMessage()
        {
            var lastPacket = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.InstallationOnlyLockMode, lastPacket?.Message);
        }

        private void ShouldReceiveAlreadyHaveObjectMessage()
        {
            var lastPacket = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.YouAlreadyHaveThisMinilandObject, lastPacket?.Message);
        }
    }
}

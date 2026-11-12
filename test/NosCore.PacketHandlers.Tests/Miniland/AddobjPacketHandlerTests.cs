//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
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
        private TestWorld World;
        private AddobjPacketHandler _addobjPacketHandler;
        private ClientSession _session;
        private IMinilandService _minilandProvider;
        private InventoryItemInstance _minilandObject;
        private ItemGenerationService _itemProvider;

        private List<ItemDto> MinilandItems => new()
        {
            new Item { Type = NoscorePocketType.Miniland, VNum = 3000, ItemType = ItemType.Minigame, MinilandObjectPoint = 100 }
        };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            await World.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto()
            {
                OwnerId = _session.Character.CharacterId,
            });
            _minilandProvider = new MinilandService(World.MapInstanceAccessorService,
                World.FriendHub.Object,
                new List<MapDto> {new Map
                {
                    MapId = 20001,
                    NameI18NKey = "miniland",
                    Data = new byte[] {}
                }},
                World.MinilandDao,
                World.MinilandObjectDao, World.Create<MinilandRegistry>());
            await _minilandProvider.InitializeAsync(_session.Character, World.MapInstanceGeneratorService);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            var mapInstance = World.GetMap(miniland.MapInstanceId);
            _session.Character.MapInstance = mapInstance;
            _addobjPacketHandler = new AddobjPacketHandler(_minilandProvider);
            _itemProvider = new ItemGenerationService(
                MinilandItems,
                NullLoggerFactory.Instance,
                World.LogLanguageLocalizer, new RarityPointService());
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

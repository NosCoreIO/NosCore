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
    public class RmvobjPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private RmvobjPacketHandler _rmvobjPacketHandler = null!;
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
                .ConstructUsing(src => new MapNpc(null, Logger, TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock));
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
            _rmvobjPacketHandler = new RmvobjPacketHandler(_minilandProvider);
            _itemProvider = new ItemGenerationService(
                MinilandItems,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()),
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task RemoveObjectWithoutItemShouldFail()
        {
            await new Spec("Remove object without item should fail")
                .Given(MinilandIsLocked)
                .WhenAsync(RemovingNonExistentObject)
                .Then(NothingShouldChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemoveObjectWhenMinilandNotLockedShouldFail()
        {
            await new Spec("Remove object when miniland not locked should fail")
                .Given(MinilandObjectExistsAndPlaced)
                .And(MinilandIsOpen)
                .WhenAsync(RemovingMinilandObject)
                .Then(ShouldReceiveRemoveOnlyLockModeMessage)
                .And(ObjectShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemoveObjectWhenLockedShouldSucceed()
        {
            await new Spec("Remove object when locked should succeed")
                .Given(MinilandObjectExistsAndPlaced)
                .And(MinilandIsLocked)
                .WhenAsync(RemovingMinilandObject)
                .Then(ObjectShouldBeRemoved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemoveNonPlacedObjectShouldFail()
        {
            await new Spec("Remove non placed object should fail")
                .Given(MinilandObjectExistsButNotPlaced)
                .And(MinilandIsLocked)
                .WhenAsync(RemovingMinilandObject)
                .Then(NothingShouldChange)
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

        private void MinilandObjectExistsAndPlaced()
        {
            var item = _itemProvider.Create(3000, 1);
            _minilandObject = InventoryItemInstance.Create(item, _session.Character.CharacterId);
            _minilandObject.Type = NoscorePocketType.Miniland;
            _minilandObject.Slot = 0;
            _session.Character.InventoryService[_minilandObject.Id] = _minilandObject;

            var mapObject = new MapDesignObject
            {
                MinilandObjectId = Guid.NewGuid(),
                MapX = 5,
                MapY = 5
            };
            _minilandProvider.AddMinilandObject(mapObject, _session.Character.CharacterId, _minilandObject);
        }

        private void MinilandObjectExistsButNotPlaced()
        {
            var item = _itemProvider.Create(3000, 1);
            _minilandObject = InventoryItemInstance.Create(item, _session.Character.CharacterId);
            _minilandObject.Type = NoscorePocketType.Miniland;
            _minilandObject.Slot = 0;
            _session.Character.InventoryService[_minilandObject.Id] = _minilandObject;
        }

        private async Task RemovingNonExistentObject()
        {
            var rmvobjPacket = new RmvobjPacket
            {
                Slot = 99
            };
            await _rmvobjPacketHandler.ExecuteAsync(rmvobjPacket, _session);
        }

        private async Task RemovingMinilandObject()
        {
            var rmvobjPacket = new RmvobjPacket
            {
                Slot = 0
            };
            await _rmvobjPacketHandler.ExecuteAsync(rmvobjPacket, _session);
        }

        private void NothingShouldChange()
        {
            var msgiPacket = _session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsNull(msgiPacket);
        }

        private void ObjectShouldStillExist()
        {
            Assert.AreEqual(1, _session.Character.MapInstance.MapDesignObjects.Count);
        }

        private void ObjectShouldBeRemoved()
        {
            Assert.AreEqual(0, _session.Character.MapInstance.MapDesignObjects.Count);
        }

        private void ShouldReceiveRemoveOnlyLockModeMessage()
        {
            var lastPacket = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.RemoveOnlyLockMode, lastPacket?.Message);
        }
    }
}

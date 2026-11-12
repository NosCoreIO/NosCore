//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
using NosCore.PacketHandlers.Miniland.MinilandObjects;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Services.UpgradeService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Miniland.MinilandObjects
{
    [TestClass]
    public class MgPacketHandlerTests
    {
        private TestWorld World;
        private MgPacketHandler _mgPacketHandler;
        private ClientSession _session;
        private IMinilandService _minilandProvider;
        private InventoryItemInstance _minilandObject;
        private ItemGenerationService _itemProvider;

        private List<ItemDto> MinilandItems => new()
        {
            new Item { Type = NoscorePocketType.Miniland, VNum = 3000, ItemType = ItemType.Minigame, MinilandObjectPoint = 100, IsWarehouse = false, EquipmentSlot = EquipmentType.Amulet, MinigameType = MinigameType.Sawmill },
            new Item { Type = NoscorePocketType.Miniland, VNum = 3001, ItemType = ItemType.Minigame, MinilandObjectPoint = 100, IsWarehouse = true },
            new Item { Type = NoscorePocketType.Main, VNum = 1269, Effect = ItemEffectType.MinigameDurabilityCoupon, EffectValue = 300 },
            new Item { Type = NoscorePocketType.Main, VNum = 1271, Effect = ItemEffectType.ProductionCoupon, EffectValue = 500 },
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
            _itemProvider = new ItemGenerationService(
                MinilandItems,
                NullLoggerFactory.Instance,
                World.LogLanguageLocalizer);
            _mgPacketHandler = new MgPacketHandler(_minilandProvider, _itemProvider, MinigameRewards);
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
        public async Task PlayingStartsTheMinigameOfTheObject()
        {
            await new Spec("Playing starts the minigame of the object")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasPoints)
                .WhenAsync(PlayingMinigame)
                .Then(TheStartedMinigameShouldBe_, MinigameType.Sawmill)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AScoreEarnsTheBoxOfItsBand()
        {
            await new Spec("A score earns the box of its band")
                .Given(MinigameObjectExistsAndPlaced)
                .WhenAsync(Scoring_, 12000)
                .Then(TheEarnedBoxShouldBe_, 2)
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

        [TestMethod]
        public async Task ADurabilityCouponRepairsTheMinigame()
        {
            await new Spec("A durability coupon adds its points to the minigame's durability")
                .Given(MinigameObjectExistsAndPlaced)
                .And(CharacterHolds_, (short)1269)
                .WhenAsync(UsingACoupon)
                .Then(DurabilityShouldBe_, 1300)
                .And(CharacterShouldHold_Of_, 0, (short)1269)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AProductionCouponIsNotARepairCoupon()
        {
            await new Spec("Without a durability coupon nothing is spent and the client is told there are none")
                .Given(MinigameObjectExistsAndPlaced)
                .And(CharacterHolds_, (short)1271)
                .WhenAsync(UsingACoupon)
                .Then(DurabilityShouldBe_, 1000)
                .And(CharacterShouldHold_Of_, 1, (short)1271)
                .And(NoCouponsMessageShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterHolds_(short vnum) =>
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_itemProvider.Create(vnum, 1), _session.Character.CharacterId));

        private Task UsingACoupon() =>
            _mgPacketHandler.ExecuteAsync(new MinigamePacket
            {
                Type = MinigamePacketType.UseCoupon,
                Id = 0,
                MinigameVNum = 3000
            }, _session);

        private void DurabilityShouldBe_(int expected) =>
            Assert.AreEqual(expected, _minilandObject.ItemInstance.DurabilityPoint);

        private void CharacterShouldHold_Of_(int expected, short vnum) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(vnum));

        private void NoCouponsMessageShouldBeSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<InfoiPacket>().Any(p => p.Message == Game18NConstString.NoCoupons));

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

        private void MinilandHasPoints() =>
            _minilandProvider.GetMiniland(_session.Character.CharacterId).MinilandPoint = 2000;

        private void MinilandHasZeroPoints()
        {
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            miniland.MinilandPoint = 0;
        }

        private async Task PlayingMinigameWithoutObject()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = MinigamePacketType.Play,
                Id = 99,
                MinigameVNum = 3000
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private async Task PlayingMinigameOnWarehouse()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = MinigamePacketType.Play,
                Id = 0,
                MinigameVNum = 3001
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private async Task PlayingMinigame()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = MinigamePacketType.Play,
                Id = 0,
                MinigameVNum = 3000
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private async Task ShowingManagement()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = MinigamePacketType.ShowManagement,
                Id = 0,
                MinigameVNum = 3000
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private async Task ShowingGifts()
        {
            var minigamePacket = new MinigamePacket
            {
                Type = MinigamePacketType.ShowGifts,
                Id = 0,
                MinigameVNum = 3000
            };
            await _mgPacketHandler.ExecuteAsync(minigamePacket, _session);
        }

        private Task Scoring_(int score) =>
            _mgPacketHandler.ExecuteAsync(new MinigamePacket
            {
                Type = MinigamePacketType.ShowBoxLevels,
                Id = 0,
                MinigameVNum = 3000,
                Point = score
            }, _session);

        private void TheStartedMinigameShouldBe_(MinigameType expected) =>
            Assert.AreEqual((short)expected, _session.LastPackets.OfType<MloStPacket>().Single().Game);

        private void TheEarnedBoxShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.LastPackets.OfType<MloLvPacket>().Single().Level);

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

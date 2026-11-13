//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Mapster;
using NodaTime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.CharacterService;
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
        private MapDesignObject _mapObject;

        private List<ItemDto> MinilandItems => new()
        {
            new Item { Type = NoscorePocketType.Miniland, VNum = 3000, ItemType = ItemType.Minigame, MinilandObjectPoint = 2000, IsWarehouse = false, EquipmentSlot = EquipmentType.Amulet, MinigameType = MinigameType.Sawmill },
            new Item { Type = NoscorePocketType.Miniland, VNum = 3001, ItemType = ItemType.Minigame, MinilandObjectPoint = 100, IsWarehouse = true },
            new Item { Type = NoscorePocketType.Main, VNum = 1269, Effect = ItemEffectType.MinigameDurabilityCoupon, EffectValue = 300 },
            new Item { Type = NoscorePocketType.Main, VNum = 1271, Effect = ItemEffectType.ProductionCoupon, EffectValue = 500 },
            new Item { Type = NoscorePocketType.Etc, VNum = 393, Effect = ItemEffectType.MinigameChest, EffectValue = 1 },
            new Item { Type = NoscorePocketType.Etc, VNum = 392, Effect = ItemEffectType.MinigameChest, EffectValue = 0 },
        };

        private static MinigameRewardService MinigameRewards => new(
            new[] { 999, 4999, 9999, 13999, 17999, 1000000 }
                .Select((max, level) => new MinigameScoreDto
                    { MinigameScoreId = (short)(10 + level), MinigameType = MinigameType.Sawmill, Level = (byte)level, MaximumScore = max })
                .ToList(),
            Enumerable.Range(0, 5)
                .Select(level => new MinigameRewardDto
                    { MinigameRewardId = (short)level, MinigameVNum = 3000, Level = (byte)level, ItemVNum = 1269, Amount = 2 })
                .ToList(),
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
                World.LogLanguageLocalizer, new RarityPointService());
            _mgPacketHandler = new MgPacketHandler(_minilandProvider, _itemProvider, MinigameRewards, World.Clock,
                new ItemCatalog(MinilandItems));
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
        public async Task ConfirmingWithoutPointsStartsTheMinigameOfTheObject()
        {
            await new Spec("Confirming without points starts the minigame of the object")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasZeroPoints)
                .WhenAsync(ConfirmingTheQuestion)
                .Then(TheStartedMinigameShouldBe_, MinigameType.Sawmill)
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
                .And(MinilandHasPoints)
                .AndAsync(PlayingMinigame)
                .And(GameRunsForSeconds_, 60)
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

        [TestMethod]
        public async Task ScoringWithoutAStartedGameEarnsNothing()
        {
            await new Spec("Scoring without a started game earns nothing")
                .Given(MinigameObjectExistsAndPlaced)
                .WhenAsync(Scoring_, 999999)
                .Then(NoBoxShouldBeEarned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ScoringTooSoonAfterTheStartEarnsNothing()
        {
            await new Spec("Scoring too soon after the start earns nothing")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasPoints)
                .AndAsync(PlayingMinigame)
                .And(GameRunsForSeconds_, 5)
                .WhenAsync(Scoring_, 999999)
                .Then(NoBoxShouldBeEarned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SelectingAGiftWithoutAFinishedGameGrantsNothing()
        {
            await new Spec("Selecting a gift without a finished game grants nothing")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasPoints)
                .WhenAsync(SelectingGift_, 1)
                .Then(RewardsShouldBeGranted_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFinishedGameGrantsOneRewardUpToTheEarnedBox()
        {
            await new Spec("A finished game grants one reward up to the earned box")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasPoints)
                .AndAsync(PlayingMinigame)
                .And(GameRunsForSeconds_, 60)
                .AndAsync(Scoring_, 5000)
                .WhenAsync(SelectingGift_, 2)
                .AndAsync(SelectingGift_, 1)
                .AndAsync(SelectingGift_, 1)
                .Then(RewardsShouldBeGranted_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefillWithANegativeAmountIsIgnored()
        {
            await new Spec("Refill with a negative amount is ignored")
                .Given(MinigameObjectExistsAndPlaced)
                .And(CharacterHasGold_, 0L)
                .WhenAsync(Refilling_, -100000)
                .Then(GoldShouldBe_, 0L)
                .And(DurabilityShouldBe_, 1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefillPastTheMaximumDurabilityIsIgnored()
        {
            await new Spec("Refill past the maximum durability is ignored")
                .Given(MinigameObjectExistsAndPlaced)
                .And(CharacterHasGold_, 1000000L)
                .WhenAsync(Refilling_, 200000)
                .Then(GoldShouldBe_, 1000000L)
                .And(DurabilityShouldBe_, 1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefillCostingMoreThanTheGoldHeldIsIgnored()
        {
            await new Spec("Refill costing more than the gold held is ignored")
                .Given(MinigameObjectExistsAndPlaced)
                .And(CharacterHasGold_, 100L)
                .WhenAsync(Refilling_, 50000)
                .Then(GoldShouldBe_, 100L)
                .And(DurabilityShouldBe_, 1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefillCostsOneHundredGoldPerPoint()
        {
            await new Spec("Refill costs one hundred gold per point")
                .Given(MinigameObjectExistsAndPlaced)
                .And(CharacterHasGold_, 1000000L)
                .WhenAsync(Refilling_, 50000)
                .Then(GoldShouldBe_, 950000L)
                .And(DurabilityShouldBe_, 1500)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OpeningAGiftBatchEmptiesThatLevelsBoxes()
        {
            await new Spec("Opening a gift batch empties that level's boxes")
                .Given(MinigameObjectExistsAndPlaced)
                .And(ObjectHasLevel1Boxes_, 2)
                .WhenAsync(OpeningGiftBatch)
                .Then(Level1BoxesShouldBe_, 0)
                .And(CharacterShouldHold_Of_, 4, (short)1269)
                .And(ReceivedItemMessageShouldSay_, 4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OpeningAGiftBatchOfAnotherPlayersObjectGrantsNothing()
        {
            await new Spec("Opening a gift batch of another player's object grants nothing")
                .Given(MinigameObjectOfAnotherPlayerExistsAndPlaced)
                .And(ObjectHasLevel1Boxes_, 2)
                .WhenAsync(OpeningGiftBatch)
                .Then(Level1BoxesShouldBe_, 2)
                .And(NothingShouldHappen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayingWithFewerPointsThanARewardCostsAsksConfirmation()
        {
            await new Spec("Playing with fewer points than a reward costs asks confirmation")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasPoints_, 50L)
                .WhenAsync(PlayingMinigame)
                .Then(ShouldReceiveConfirmationQuestion)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClaimingARewardSpendsItsPoints()
        {
            await new Spec("Claiming a reward spends its points")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasPoints)
                .AndAsync(PlayingMinigame)
                .And(GameRunsForSeconds_, 60)
                .AndAsync(Scoring_, 5000)
                .WhenAsync(SelectingGift_, 1)
                .Then(RewardsShouldBeGranted_, 1)
                .And(MinilandPointsShouldBe_, 1900L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClaimingARewardWearsTheObjectByItsLevel()
        {
            await new Spec("Claiming a reward wears the object by its level")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasPoints)
                .AndAsync(PlayingMinigame)
                .And(GameRunsForSeconds_, 60)
                .AndAsync(Scoring_, 5000)
                .WhenAsync(SelectingGift_, 1)
                .Then(DurabilityShouldBe_, 985)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClaimingARewardWithoutEnoughPointsIsRefused()
        {
            await new Spec("Claiming a reward without enough points is refused")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandHasPoints)
                .AndAsync(PlayingMinigame)
                .And(GameRunsForSeconds_, 60)
                .AndAsync(Scoring_, 5000)
                .And(MinilandHasPoints_, 50L)
                .WhenAsync(SelectingGift_, 1)
                .Then(RewardsShouldBeGranted_, 0)
                .And(MinilandPointsShouldBe_, 50L)
                .And(DurabilityShouldBe_, 1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFreshOwnerEarnsARewardFromTheDailyAllowance()
        {
            await new Spec("A fresh owner earns a reward from the daily allowance")
                .Given(MinigameObjectExistsAndPlaced)
                .And(MinilandPointsWereNeverGranted)
                .AndAsync(OwnerLogsIn)
                .AndAsync(PlayingMinigame)
                .And(GameRunsForSeconds_, 60)
                .AndAsync(Scoring_, 5000)
                .WhenAsync(SelectingGift_, 1)
                .Then(RewardsShouldBeGranted_, 1)
                .And(MinilandPointsShouldBe_, 1900L)
                .ExecuteAsync();
        }

        private void MinilandHasPoints_(long points) =>
            _minilandProvider.GetMiniland(_session.Character.CharacterId).MinilandPoint = points;

        private void MinilandPointsWereNeverGranted()
        {
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            miniland.MinilandPoint = 0;
            miniland.MinilandPointRefreshedAt = null;
        }

        private Task OwnerLogsIn() =>
            new CharacterInitializationService(_minilandProvider, World.MapInstanceGeneratorService, World.Clock)
                .InitializeAsync(_session.Character);

        private void MinilandPointsShouldBe_(long points) =>
            Assert.AreEqual(points, _minilandProvider.GetMiniland(_session.Character.CharacterId).MinilandPoint);

        private void GameRunsForSeconds_(int seconds) => World.Clock.Advance(Duration.FromSeconds(seconds));

        private void CharacterHasGold_(long gold) => _session.Character.Gold = gold;

        private void ObjectHasLevel1Boxes_(int amount) => _mapObject.Level1BoxAmount = (byte)amount;

        private Task Send_(MinigamePacketType type, int? point) =>
            _mgPacketHandler.ExecuteAsync(new MinigamePacket
            {
                Type = type,
                Id = 0,
                MinigameVNum = 3000,
                Point = point
            }, _session);

        private Task SelectingGift_(int tier) => Send_(MinigamePacketType.SelectGift, tier);

        private Task Refilling_(int gold) => Send_(MinigamePacketType.Refill, gold);

        private Task OpeningGiftBatch() => Send_(MinigamePacketType.OpenGiftBatch, 0);

        private void ReceivedItemMessageShouldSay_(int amount) =>
            Assert.AreEqual(amount.ToString(), _session.LastPackets.OfType<SayiPacket>()
                .Single(p => p.Message == Game18NConstString.ReceivedThisItem).Game18NArguments[1]?.ToString());

        private void GoldShouldBe_(long gold) => Assert.AreEqual(gold, _session.Character.Gold);

        private void NoBoxShouldBeEarned() => Assert.IsFalse(_session.LastPackets.OfType<MloLvPacket>().Any());

        private void RewardsShouldBeGranted_(int count) =>
            Assert.AreEqual(count, _session.LastPackets.OfType<MloRwPacket>().Count());

        private void Level1BoxesShouldBe_(int amount) => Assert.AreEqual(amount, _mapObject.Level1BoxAmount);

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

        private void MinigameObjectExistsAndPlaced() => PlaceMinigame(_session.Character.CharacterId, 1000);

        private void MinigameObjectExistsAndPlacedWithZeroDurability() => PlaceMinigame(_session.Character.CharacterId, 0);

        private void MinigameObjectOfAnotherPlayerExistsAndPlaced() => PlaceMinigame(_session.Character.CharacterId + 1, 1000);

        private void PlaceMinigame(long ownerId, int durability)
        {
            var item = _itemProvider.Create(3000, 1);
            item.DurabilityPoint = durability;
            _minilandObject = InventoryItemInstance.Create(item, ownerId);
            _minilandObject.Type = NoscorePocketType.Miniland;
            _minilandObject.Slot = 0;
            if (ownerId == _session.Character.CharacterId)
            {
                _session.Character.InventoryService[_minilandObject.Id] = _minilandObject;
            }

            _mapObject = new MapDesignObject
            {
                MinilandObjectId = Guid.NewGuid(),
                MapX = 5,
                MapY = 5,
                Slot = 0
            };
            _minilandProvider.AddMinilandObject(_mapObject, _session.Character.CharacterId, _minilandObject);
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

        private async Task ConfirmingTheQuestion()
        {
            await PlayingMinigame();
            var question = _session.LastPackets.OfType<QnaiPacket>().Single();
            _session.LastPackets.Clear();
            await _mgPacketHandler.ExecuteAsync((MinigamePacket)question.YesPacket.NotNull(), _session);
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
            var yes = (MinigamePacket)lastPacket.YesPacket.NotNull();
            Assert.AreEqual(0, yes.Id);
            Assert.AreEqual(3000, yes.MinigameVNum);
            Assert.AreEqual(1, yes.Point);
            Assert.IsFalse(_session.LastPackets.OfType<MloStPacket>().Any());
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
            CollectionAssert.AreEqual(new short[] { 392, 393 },
                lastPacket.MloPmgSubPackets.NotNull().Take(2).Select(box => box.NotNull().BoxVNum).ToArray());
        }
    }
}

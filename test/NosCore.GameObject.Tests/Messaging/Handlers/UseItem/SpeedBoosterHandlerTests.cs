//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class SpeedBoosterHandlerTests
    {
        private TestWorld World;
        private const short BoosterVNum = 5119;
        private const short MountVNum = 1906;
        private const short BoostCardId = 336;

        private ClientSession _session;
        private BuffService _buffs;
        private ISpeedCalculationService _speed;
        private SpeedBoosterHandler _handler;
        private InventoryItemInstance _booster;
        private const short LowDebuff = 900;
        private const short HighDebuff = 901;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetItemCard(BoosterVNum)).Returns(new CardDto { CardId = BoostCardId });
            cards.Setup(c => c.GetCardBCards(BoostCardId)).Returns(new List<BCardDto>());
            cards.Setup(c => c.IsGrantedBy(BoostCardId, ItemEffectType.SpeedBooster)).Returns(true);
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _speed = World.Create<SpeedCalculationService>();
            World.Inject(_speed);
            _handler = World.Create<SpeedBoosterHandler>();
        }

        [TestMethod]
        public async Task OnFootTheBoosterIsKept()
        {
            GiveBoosters(1);

            await UseAsync();

            Assert.IsFalse(_buffs.HasBuff(_session.Character, BoostCardId));
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(BoosterVNum));
        }

        [TestMethod]
        public async Task AMountWithoutABoostKeepsTheBooster()
        {
            Mount(speed: 21, boost: 0, seconds: 0);
            GiveBoosters(1);

            await UseAsync();

            Assert.AreEqual(21, _session.Character.Speed);
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(BoosterVNum));
        }

        [TestMethod]
        public async Task TheMountRunsFasterForItsBoostTimeAndTheBoosterIsUsedUp()
        {
            Mount(speed: 20, boost: 2, seconds: 3);
            GiveBoosters(1);

            await UseAsync();

            Assert.AreEqual(22, _session.Character.Speed);
            var buff = _buffs.GetActiveBuffs(_session.Character).Single(b => b.CardId == BoostCardId);
            Assert.AreEqual(3, (int)(buff.ExpiresAt - World.Clock.GetCurrentInstant()).TotalSeconds);
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(BoosterVNum));
        }

        [TestMethod]
        public async Task ARestoringMountHealsByTheCharactersLevel()
        {
            Mount(new ItemDto { VNum = MountVNum, Speed = 20, SpeedBoost = 3, SpeedBoostDuration = 5, SpeedBoostRestoresPerLevel = 15 });
            var character = _session.Character;
            character.Hp = 1;
            character.Mp = 1;
            GiveBoosters(1);

            await UseAsync();

            Assert.AreEqual(Math.Min(character.MaxHp, 1 + character.Level * 15), character.Hp);
            Assert.AreEqual(Math.Min(character.MaxMp, 1 + character.Level * 15), character.Mp);
        }

        [TestMethod]
        public async Task AHigherLevelRiderIsHealedMoreButNotPastTheMaximum()
        {
            Mount(new ItemDto { VNum = MountVNum, Speed = 20, SpeedBoost = 3, SpeedBoostDuration = 5, SpeedBoostRestoresPerLevel = 15 });
            var character = _session.Character;
            character.Level = 10;
            character.Hp = character.MaxHp - 100;
            character.Mp = 1;
            GiveBoosters(1);

            await UseAsync();

            Assert.AreEqual(character.MaxHp, character.Hp);
            Assert.AreEqual(Math.Min(character.MaxMp, 151), character.Mp);
        }

        [TestMethod]
        public async Task AUnicornWithNowhereToLandKeepsTheBooster()
        {
            Mount(new ItemDto { VNum = MountVNum, Speed = 22, SpeedBoostTeleportsRandomly = true });
            var random = World.Freeze<Mock<IRandomProvider>>();
            random.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(1);
            _handler = World.Create<SpeedBoosterHandler>();
            GiveBoosters(1);

            await UseAsync();

            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(BoosterVNum));
        }

        [TestMethod]
        public async Task ACleansingMountRemovesLowDebuffsOnly()
        {
            Mount(new ItemDto { VNum = MountVNum, Speed = 21, SpeedBoost = 3, SpeedBoostDuration = 5, SpeedBoostRemovesDebuffsUpToLevel = 4 });
            var cards = World.Freeze<Mock<ICardCatalog>>();
            var low = new CardDto { CardId = LowDebuff, Level = 4, BuffType = BuffType.Bad, Duration = 600 };
            var high = new CardDto { CardId = HighDebuff, Level = 5, BuffType = BuffType.Bad, Duration = 600 };
            cards.Setup(c => c.GetCard(LowDebuff)).Returns(low);
            cards.Setup(c => c.GetCard(HighDebuff)).Returns(high);
            await _buffs.ApplyAsync(_session.Character, low, new List<BCardDto>(), null);
            await _buffs.ApplyAsync(_session.Character, high, new List<BCardDto>(), null);
            GiveBoosters(1);

            await UseAsync();

            Assert.IsFalse(_buffs.HasBuff(_session.Character, LowDebuff));
            Assert.IsTrue(_buffs.HasBuff(_session.Character, HighDebuff));
        }

        [TestMethod]
        public async Task AUnicornSkipsAWallAndLandsOnAWalkableCell()
        {
            Mount(new ItemDto { VNum = MountVNum, Speed = 22, SpeedBoostTeleportsRandomly = true });
            var random = World.Freeze<Mock<IRandomProvider>>();
            random.SetupSequence(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(1).Returns(1).Returns(0).Returns(1);
            _handler = World.Create<SpeedBoosterHandler>();
            GiveBoosters(1);

            await UseAsync();

            Assert.AreEqual((short)0, _session.Character.PositionX);
            Assert.AreEqual((short)1, _session.Character.PositionY);
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(BoosterVNum));
        }

        [TestMethod]
        public async Task ADashingMountMovesTheRiderForwards()
        {
            Mount(new ItemDto { VNum = MountVNum, Speed = 21, SpeedBoost = 2, SpeedBoostDuration = 3, SpeedBoostDashCells = 5 });
            await _session.Character.TeleportAsync(4, 0);
            _session.Character.Direction = 2;
            GiveBoosters(1);

            await UseAsync();

            Assert.AreEqual(((short)4, (short)5), (_session.Character.PositionX, _session.Character.PositionY));
        }

        [TestMethod]
        public async Task ADashStopsBeforeAWall()
        {
            Mount(new ItemDto { VNum = MountVNum, Speed = 21, SpeedBoost = 2, SpeedBoostDuration = 3, SpeedBoostDashCells = 5 });
            await _session.Character.TeleportAsync(6, 2);
            _session.Character.Direction = 3;
            GiveBoosters(1);

            await UseAsync();

            Assert.AreEqual(((short)4, (short)2), (_session.Character.PositionX, _session.Character.PositionY));
        }

        [TestMethod]
        public async Task ABoostedMountKeepsTheSecondBooster()
        {
            Mount(speed: 20, boost: 2, seconds: 3);
            GiveBoosters(2);
            await UseAsync();

            await UseAsync();

            Assert.AreEqual(22, _session.Character.Speed);
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(BoosterVNum));
        }

        [TestMethod]
        public async Task TheMountIsBackToItsOwnSpeedOnceTheBoostRunsOut()
        {
            Mount(speed: 20, boost: 2, seconds: 3);
            GiveBoosters(1);
            await UseAsync();

            World.Clock.Advance(Duration.FromSeconds(4));
            await _buffs.TickAsync(_session.Character);

            Assert.AreEqual(20, _speed.CalculateSpeed(_session.Character));
        }

        private void Mount(byte speed, byte boost, byte seconds) =>
            Mount(new ItemDto { VNum = MountVNum, Speed = speed, SpeedBoost = boost, SpeedBoostDuration = seconds });

        private void Mount(ItemDto mount)
        {
            World.ItemList.RemoveAll(i => i.VNum == MountVNum);
            World.ItemList.Add(mount);
            var character = _session.Character;
            character.IsVehicled = true;
            character.VehicleSpeed = mount.Speed;
            character.VehicleVNum = MountVNum;
            character.Speed = _speed.CalculateSpeed(character);
        }

        private void GiveBoosters(short amount)
        {
            var item = new Item
            {
                VNum = BoosterVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = ItemEffectType.SpeedBooster,
            };
            var instance = new ItemInstanceForTest(BoosterVNum) { Amount = amount, Item = item };
            _booster = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _booster.Slot = 5;
            _booster.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_booster.ItemInstanceId] = _booster;
        }

        private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _booster, new UseItemPacket
        {
            VisualType = NosCore.Shared.Enumerations.VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = 5,
            Mode = 1,
            Parameter = 0,
        }));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

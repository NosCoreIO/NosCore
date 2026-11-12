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
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
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

        private ClientSession _session;
        private BuffService _buffs;
        private ISpeedCalculationService _speed;
        private SpeedBoosterHandler _handler;
        private InventoryItemInstance _booster;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetCard(SpeedBooster.CardId)).Returns(new CardDto { CardId = SpeedBooster.CardId });
            cards.Setup(c => c.GetCardBCards(SpeedBooster.CardId)).Returns(new List<BCardDto>());
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

            Assert.IsFalse(_buffs.HasBuff(_session.Character, SpeedBooster.CardId));
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
            var buff = _buffs.GetActiveBuffs(_session.Character).Single(b => b.CardId == SpeedBooster.CardId);
            Assert.AreEqual(3, (int)(buff.ExpiresAt - World.Clock.GetCurrentInstant()).TotalSeconds);
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(BoosterVNum));
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

        private void Mount(byte speed, byte boost, byte seconds)
        {
            var character = _session.Character;
            character.IsVehicled = true;
            character.VehicleSpeed = speed;
            character.VehicleSpeedBoost = boost;
            character.VehicleSpeedBoostSeconds = seconds;
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

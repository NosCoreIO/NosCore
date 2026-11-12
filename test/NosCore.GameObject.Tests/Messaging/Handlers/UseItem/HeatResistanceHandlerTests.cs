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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class HeatResistanceHandlerTests
    {
        private TestWorld World;
        private const short OilVNum = 5929;

        private ClientSession _session;
        private BuffService _buffs;
        private HeatResistanceHandler _handler;
        private InventoryItemInstance _oil;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetCard(HeatResistance.CardId)).Returns(new CardDto { CardId = HeatResistance.CardId });
            cards.Setup(c => c.GetCard(HeatResistance.BurningCardId))
                .Returns(new CardDto { CardId = HeatResistance.BurningCardId, Propability = 2, Duration = 600 });
            cards.Setup(c => c.GetCardBCards(It.IsAny<short>())).Returns(new List<BCardDto>());
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _handler = World.Create<HeatResistanceHandler>();
        }

        [TestMethod]
        public async Task IceFlowerOilGivesTenMinutesAndIsUsedUp()
        {
            GiveOil(ItemEffectType.HeatResistance, 600, 1);

            await UseAsync();

            Assert.AreEqual(600, SecondsLeft());
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(OilVNum));
        }

        [TestMethod]
        public async Task TheCharacterWindowIsRefreshedWithTheOil()
        {
            GiveOil(ItemEffectType.HeatResistance, 600, 1);

            await UseAsync();

            Assert.IsTrue(_session.LastPackets.OfType<ScPacket>().Any());
        }

        [TestMethod]
        public async Task EachOilAddsTenMinutesUpToTwoHours()
        {
            GiveOil(ItemEffectType.HeatResistance, 600, 3);
            await UseAsync();
            World.Clock.Advance(Duration.FromMinutes(4));

            await UseAsync();

            Assert.AreEqual(960, SecondsLeft());
        }

        [TestMethod]
        public async Task StrongOilTopsUpToTwoHours()
        {
            GiveOil(ItemEffectType.HeatResistance, 600, 1);
            await UseAsync();
            GiveOil(ItemEffectType.HeatResistance, 7200, 1);

            await UseAsync();

            Assert.AreEqual(7200, SecondsLeft());
        }

        [TestMethod]
        public async Task AFullTwoHoursKeepsTheOil()
        {
            GiveOil(ItemEffectType.HeatResistance, 7200, 2);
            await UseAsync();

            await UseAsync();

            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(OilVNum));
        }

        [TestMethod]
        public async Task TheOilCoolsOffTheBurningSword()
        {
            var cards = World.Freeze<Mock<ICardCatalog>>();
            var burning = cards.Object.GetCard(HeatResistance.BurningCardId);
            Assert.IsNotNull(burning);
            await _buffs.ApplyAsync(_session.Character, burning, new List<BCardDto>(), null);
            Assert.IsTrue(_buffs.HasBuff(_session.Character, HeatResistance.BurningCardId));
            GiveOil(ItemEffectType.HeatResistance, 600, 1);

            await UseAsync();

            Assert.IsFalse(_buffs.HasBuff(_session.Character, HeatResistance.BurningCardId));
        }

        [TestMethod]
        public async Task AnotherEffectIsLeftToItsOwnHandler()
        {
            GiveOil(ItemEffectType.FairyBooster, 600, 1);

            await UseAsync();

            Assert.IsFalse(_buffs.HasBuff(_session.Character, HeatResistance.CardId));
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(OilVNum));
        }

        private int SecondsLeft() =>
            (int)(_buffs.GetActiveBuffs(_session.Character).Single(b => b.CardId == HeatResistance.CardId).ExpiresAt
                - World.Clock.GetCurrentInstant()).TotalSeconds;

        private void GiveOil(ItemEffectType effect, int seconds, short amount)
        {
            var item = new Item
            {
                VNum = OilVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = effect,
                EffectValue = seconds,
            };
            var instance = new ItemInstanceForTest(OilVNum) { Amount = amount, Item = item };
            _oil = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _oil.Slot = 5;
            _oil.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_oil.ItemInstanceId] = _oil;
        }

        private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _oil, new UseItemPacket
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

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
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class FoodHandlerTests
    {
        private const short FoodVNum = 2017;
        private ClientSession _session = null!;
        private Mock<ICardCatalog> _cards = null!;
        private FoodHandler _handler = null!;
        private InventoryItemInstance _food = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MaxHp = 5000;
            _session.Character.MaxMp = 5000;
            _session.Character.Hp = 1000;
            _session.Character.Mp = 1000;
            _cards = new Mock<ICardCatalog>();
            _cards.Setup(c => c.GetItemBCards(It.IsAny<short>())).Returns(new List<BCardDto>());
            _handler = new FoodHandler(TestHelpers.Instance.Clock, _cards.Object);
        }

        [TestMethod]
        public async Task FoodWhileStandingSitsYouDown()
        {
            await new Spec("Food used while standing sits the character down, plays the eat effect and is used up")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 700, 700)
                .And(TheCharacterIsStanding)
                .WhenAsync(EatingIt)
                .Then(TheCharacterShouldBeSitting)
                .And(ItemCountShouldBe_, 0)
                .And(TheEatEffectShouldBeShown)
                .And(TheFoodScheduleShouldNextHeal_, 140)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FoodWhileSittingKeepsYouSitting()
        {
            await new Spec("Eating while already resting does not toggle the character back up")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 700, 700)
                .And(TheCharacterIsSitting)
                .WhenAsync(EatingIt)
                .Then(TheCharacterShouldBeSitting)
                .And(ItemCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheFifthFoodSaysYouAreFullButIsStillEaten()
        {
            await new Spec("With four 700-HP foods digesting, a fifth goes past the cap: I'm full, but it is eaten")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 700, 700)
                .And(TheCharacterIsSitting)
                .And(_FoodsOf_PerTickAreDigesting, 4, 140)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 0)
                .And(TheFullMessageShouldBeSaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALoneBigFoodIsNotFull()
        {
            await new Spec("With nothing digesting, a single 5000-HP food is eaten without the full message")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 5000, 2000)
                .And(TheCharacterIsSitting)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 0)
                .And(NoFullMessageShouldBeSaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EatingWhileDigestingKeepsTheTickRhythm()
        {
            await new Spec("A second meal does not push back the next tick of the first")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 700, 700)
                .And(TheCharacterIsSitting)
                .And(_FoodsOf_PerTickAreDigesting, 1, 140)
                .And(TheLastTickWas_SecondsAgo, 1)
                .WhenAsync(EatingIt)
                .Then(TheLastTickShouldStillBe_SecondsAgo, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadCharacterCannotEat()
        {
            await new Spec("A dead character's food is kept")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 700, 700)
                .And(TheCharacterIsDead)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FoodWithABuffIsLeftToItsOwnHandler()
        {
            await new Spec("Items declaring buff cards are not flat food")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 700, 700)
                .And(TheItemDeclaresABuff)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FoodAtFullHealthIsStillEaten()
        {
            await new Spec("Food is used up even when nothing is missing")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 700, 700)
                .And(TheCharacterIsAtFullHealth)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASnackIsNotFood()
        {
            await new Spec("Snacks have their own handler")
                .Given(A_Restoring_Hp_Mp, ItemType.Snack, 250, 250)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void A_Restoring_Hp_Mp(ItemType itemType, int hp, int mp)
        {
            var item = new Item
            {
                VNum = FoodVNum,
                Type = NoscorePocketType.Main,
                ItemType = itemType,
                Hp = (short)hp,
                Mp = (short)mp,
            };
            var instance = new ItemInstanceForTest(FoodVNum) { Amount = 1, Item = item };
            _food = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _food.Slot = 0;
            _food.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_food.ItemInstanceId] = _food;
        }

        private void _FoodsOf_PerTickAreDigesting(int count, int perTick)
        {
            for (var i = 0; i < count; i++)
            {
                _session.Character.Digestion.Food.Add(perTick, perTick);
            }
        }

        private void TheCharacterIsStanding() => _session.Character.IsSitting = false;

        private void TheCharacterIsSitting() => _session.Character.IsSitting = true;

        private void TheCharacterShouldBeSitting() => Assert.IsTrue(_session.Character.IsSitting);

        private void TheCharacterIsDead() => _session.Character.IsAlive = false;

        private void TheItemDeclaresABuff() =>
            _cards.Setup(c => c.GetItemBCards(FoodVNum)).Returns(new List<BCardDto> { new() { ItemVNum = FoodVNum } });

        private void TheCharacterIsAtFullHealth()
        {
            _session.Character.Hp = _session.Character.MaxHp;
            _session.Character.Mp = _session.Character.MaxMp;
        }

        private async Task EatingIt()
        {
            _session.LastPackets.Clear();
            _session.Character.MapInstance.LastPackets.Clear();
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _food, packet));
        }

        private void ItemCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(FoodVNum));

        private void TheEatEffectShouldBeShown() =>
            Assert.IsTrue(_session.Character.MapInstance.LastPackets.OfType<EffectPacket>().Any(e => e.Id == 6000));

        private void TheFoodScheduleShouldNextHeal_(int perTick) =>
            Assert.AreEqual((perTick, perTick), _session.Character.Digestion.Food.Tick());

        private void NoFullMessageShouldBeSaid() =>
            Assert.IsFalse(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.IAmFull));

        private void TheLastTickWas_SecondsAgo(int seconds) =>
            _session.Character.Digestion.LastTick =
                TestHelpers.Instance.Clock.GetCurrentInstant() - NodaTime.Duration.FromSeconds(seconds);

        private void TheLastTickShouldStillBe_SecondsAgo(int seconds) =>
            Assert.AreEqual(TestHelpers.Instance.Clock.GetCurrentInstant() - NodaTime.Duration.FromSeconds(seconds),
                _session.Character.Digestion.LastTick);

        private void TheFullMessageShouldBeSaid()
        {
            var say = _session.LastPackets.OfType<SayiPacket>().SingleOrDefault();
            Assert.IsNotNull(say);
            Assert.AreEqual(Game18NConstString.IAmFull, say.Message);
            Assert.AreEqual(SayColorType.BubbleOnly, say.Type);
            Assert.AreEqual(_session.Character.CharacterId, say.VisualId);
        }

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

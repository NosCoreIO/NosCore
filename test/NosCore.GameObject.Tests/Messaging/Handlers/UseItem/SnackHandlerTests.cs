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
    public class SnackHandlerTests
    {
        private const short SnackVNum = 2027;
        private ClientSession _session = null!;
        private Mock<ICardCatalog> _cards = null!;
        private SnackHandler _handler = null!;
        private InventoryItemInstance _snack = null!;

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
            _handler = new SnackHandler(TestHelpers.Instance.Clock, _cards.Object);
        }

        [TestMethod]
        public async Task ASnackIsEatenAndStartsDigesting()
        {
            await new Spec("A snack plays the eat effect, is used up and queues a fifth of its value per tick")
                .Given(A_Restoring_Hp_Mp, ItemType.Snack, 250, 250)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 0)
                .And(TheEatEffectShouldBeShown)
                .And(TheSnackScheduleShouldNextHeal_, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSixthChickenMakesYouSick()
        {
            await new Spec("With five 250-HP snacks digesting, a sixth is refused with the sick message and kept")
                .Given(A_Restoring_Hp_Mp, ItemType.Snack, 250, 250)
                .And(_SnacksOf_PerTickAreDigesting, 5, 50)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 1)
                .And(TheSickMessageShouldBeSaid)
                .And(TheSnackScheduleShouldNextHeal_, 250)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALoneBigSnackIsAccepted()
        {
            await new Spec("Nothing digesting: even a 2000-HP snack is eaten")
                .Given(A_Restoring_Hp_Mp, ItemType.Snack, 2000, 2000)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 0)
                .And(TheSnackScheduleShouldNextHeal_, 400)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadCharacterCannotEat()
        {
            await new Spec("A dead character's snack is kept")
                .Given(A_Restoring_Hp_Mp, ItemType.Snack, 250, 250)
                .And(TheCharacterIsDead)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASnackWithABuffIsLeftToItsOwnHandler()
        {
            await new Spec("Items declaring buff cards are not flat snacks")
                .Given(A_Restoring_Hp_Mp, ItemType.Snack, 250, 250)
                .And(TheItemDeclaresABuff)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASnackAtFullHealthIsStillEaten()
        {
            await new Spec("Snacks are used up even when nothing is missing")
                .Given(A_Restoring_Hp_Mp, ItemType.Snack, 250, 250)
                .And(TheCharacterIsAtFullHealth)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FoodIsNotASnack()
        {
            await new Spec("Food has its own handler")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 300, 300)
                .WhenAsync(EatingIt)
                .Then(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void A_Restoring_Hp_Mp(ItemType itemType, int hp, int mp)
        {
            var item = new Item
            {
                VNum = SnackVNum,
                Type = NoscorePocketType.Main,
                ItemType = itemType,
                Hp = (short)hp,
                Mp = (short)mp,
            };
            var instance = new ItemInstanceForTest(SnackVNum) { Amount = 1, Item = item };
            _snack = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _snack.Slot = 0;
            _snack.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_snack.ItemInstanceId] = _snack;
        }

        private void _SnacksOf_PerTickAreDigesting(int count, int perTick)
        {
            for (var i = 0; i < count; i++)
            {
                _session.Character.Digestion.Snack.Add(perTick, perTick);
            }
        }

        private void TheCharacterIsDead() => _session.Character.IsAlive = false;

        private void TheItemDeclaresABuff() =>
            _cards.Setup(c => c.GetItemBCards(SnackVNum)).Returns(new List<BCardDto> { new() { ItemVNum = SnackVNum } });

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
            await _handler.Handle(new ItemUsedEvent(_session, _snack, packet));
        }

        private void ItemCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(SnackVNum));

        private void TheEatEffectShouldBeShown() =>
            Assert.IsTrue(_session.Character.MapInstance.LastPackets.OfType<EffectPacket>().Any(e => e.Id == 6000));

        private void TheSnackScheduleShouldNextHeal_(int perTick) =>
            Assert.AreEqual((perTick, perTick), _session.Character.Digestion.Snack.Tick());

        private void TheSickMessageShouldBeSaid()
        {
            var say = _session.LastPackets.OfType<SayiPacket>().SingleOrDefault();
            Assert.IsNotNull(say);
            Assert.AreEqual(Game18NConstString.FeelingSick, say.Message);
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

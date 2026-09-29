//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class PotionHandlerTests
    {
        private const short PotionVNum = 1002;
        private ClientSession _session = null!;
        private PotionHandler _handler = null!;
        private InventoryItemInstance _potion = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MaxHp = 1000;
            _session.Character.MaxMp = 800;
            _handler = new PotionHandler();
        }

        [TestMethod]
        public async Task AHealthPotionRestoresItsHpAndIsConsumed()
        {
            await new Spec("A health potion adds its Hp and is used up")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 800)
                .And(MpShouldBe_, 200)
                .And(PotionCountShouldBe_, 0)
                .And(TheStatBarShouldShow_Hp_Mp, 800, 200)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARecoveryPotionRestoresHpAndMp()
        {
            await new Spec("A recovery potion adds both its Hp and its Mp")
                .Given(APotionRestoring_Hp_Mp, 300, 300)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 400)
                .And(MpShouldBe_, 400)
                .And(PotionCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HealingStopsAtTheMaximum()
        {
            await new Spec("A potion never lifts HP or MP past the character's maximum")
                .Given(APotionRestoring_Hp_Mp, 1000, 1000)
                .And(CharacterHas_Hp_Mp, 900, 790)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 1000)
                .And(MpShouldBe_, 800)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APotionThatWouldRestoreNothingIsKept()
        {
            await new Spec("A health potion at full HP is not used up, even with MP missing")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 1000, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(PotionCountShouldBe_, 1)
                .And(NoStatBarShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadCharacterCannotDrink()
        {
            await new Spec("A dead character's potion is not used and heals nothing")
                .Given(APotionRestoring_Hp_Mp, 300, 300)
                .And(CharacterHas_Hp_Mp, 0, 0)
                .And(TheCharacterIsDead)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 0)
                .And(PotionCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APotionWithoutFlatValuesIsLeftAlone()
        {
            await new Spec("A potion carrying no Hp or Mp of its own is not handled here")
                .Given(APotionRestoring_Hp_Mp, 0, 0)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .Then(PotionCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FoodIsNotAPotion()
        {
            await new Spec("Food carries Hp and Mp too, but heals over time elsewhere")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 300, 300)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 100)
                .And(PotionCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void APotionRestoring_Hp_Mp(int hp, int mp) => A_Restoring_Hp_Mp(ItemType.Potion, hp, mp);

        private void A_Restoring_Hp_Mp(ItemType itemType, int hp, int mp)
        {
            var item = new Item
            {
                VNum = PotionVNum,
                Type = NoscorePocketType.Main,
                ItemType = itemType,
                Hp = (short)hp,
                Mp = (short)mp,
            };
            var instance = new ItemInstanceForTest(PotionVNum) { Amount = 1, Item = item };
            _potion = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _potion.Slot = 0;
            _potion.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_potion.ItemInstanceId] = _potion;
        }

        private void CharacterHas_Hp_Mp(int hp, int mp)
        {
            _session.Character.Hp = hp;
            _session.Character.Mp = mp;
        }

        private void TheCharacterIsDead() => _session.Character.IsAlive = false;

        private async Task DrinkingThePotion()
        {
            _session.LastPackets.Clear();
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _potion, packet));
        }

        private void HpShouldBe_(int expected) => Assert.AreEqual(expected, _session.Character.Hp);

        private void MpShouldBe_(int expected) => Assert.AreEqual(expected, _session.Character.Mp);

        private void PotionCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(PotionVNum));

        private void TheStatBarShouldShow_Hp_Mp(int hp, int mp)
        {
            var stat = _session.LastPackets.OfType<StatPacket>().LastOrDefault();
            Assert.IsNotNull(stat);
            Assert.AreEqual(hp, stat.Hp);
            Assert.AreEqual(mp, stat.Mp);
        }

        private void NoStatBarShouldBeSent() => Assert.IsFalse(_session.LastPackets.OfType<StatPacket>().Any());

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

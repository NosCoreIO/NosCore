//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class PetBasketHandlerTests
    {
        private const short PetBasketVNum = 1907;
        private ClientSession _session = null!;
        private PetBasketHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new PetBasketHandler(TestHelpers.Instance.WorldConfiguration, TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task UnrelatedEffectIsIgnored()
        {
            await new Spec("Item with a non-pet effect is ignored — no bonus, no consumption")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.Teleport)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusCountShouldBe_, 0)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PetSpaceUpgradeMapsToPetBasketBonus()
        {
            await new Spec("PetSpaceUpgrade effect grants StaticBonusType.PetBasket (shared with PetBasketUpgrade)")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.PetSpaceUpgrade)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusListShouldContain_, StaticBonusType.PetBasket)
                .And(ItemStackCountShouldBe_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PetBasketUpgradeGrantsPetBasketAndConsumesItem()
        {
            await new Spec("PetBasketUpgrade grants PetBasket and consumes one item")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.PetBasketUpgrade)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusListShouldContain_, StaticBonusType.PetBasket)
                .And(ItemStackCountShouldBe_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PetBackpackUpgradeGrantsPetBackPackBonus()
        {
            await new Spec("PetBackpackUpgrade maps to the separate PetBackPack bonus slot")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.PetBackpackUpgrade)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusListShouldContain_, StaticBonusType.PetBackPack)
                .And(ItemStackCountShouldBe_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DuplicatePetBasketIsRejectedWithNotInPair()
        {
            await new Spec("Second PetBasket grant emits NotInPair and does not consume the item")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.PetBasketUpgrade)
                .And(CharacterAlreadyHasStaticBonus_, StaticBonusType.PetBasket)
                .WhenAsync(UsingTheItem)
                .Then(NotInPairShouldBeSent)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DuplicatePetBackPackIsRejectedWithNotInPair()
        {
            await new Spec("Second PetBackPack grant emits NotInPair and does not consume the item")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.PetBackpackUpgrade)
                .And(CharacterAlreadyHasStaticBonus_, StaticBonusType.PetBackPack)
                .WhenAsync(UsingTheItem)
                .Then(NotInPairShouldBeSent)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PermanentBasketLeavesDateEndNull()
        {
            await new Spec("EffectValue=0 means permanent: StaticBonus.DateEnd is null")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.PetBasketUpgrade)
                .And(ItemHasEffectValueDays_, 0)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusDateEndShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TimedBasketSetsDateEnd()
        {
            await new Spec("EffectValue>0 days stamps DateEnd = now + that many days")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.PetBasketUpgrade)
                .And(ItemHasEffectValueDays_, 30)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusDateEndShouldNotBeNull)
                .ExecuteAsync();
        }

        private void ItemInInventoryWithEffect_(ItemEffectType effect)
        {
            var item = new Item
            {
                VNum = PetBasketVNum,
                Type = NoscorePocketType.Main,
                Effect = effect,
                EffectValue = 0,
            };
            var inst = new ItemInstanceForTest(PetBasketVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void ItemHasEffectValueDays_(int days) =>
            _item.ItemInstance.Item.EffectValue = days;

        private void CharacterAlreadyHasStaticBonus_(StaticBonusType type) =>
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = _session.Character.CharacterId,
                StaticBonusType = type,
            });

        private async Task UsingTheItem()
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _item, packet));
        }

        private void StaticBonusCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.StaticBonusList.Count);

        private void StaticBonusListShouldContain_(StaticBonusType type) =>
            Assert.IsTrue(_session.Character.StaticBonusList.Any(b => b.StaticBonusType == type));

        private void StaticBonusDateEndShouldBeNull() =>
            Assert.IsNull(_session.Character.StaticBonusList.Last().DateEnd);

        private void StaticBonusDateEndShouldNotBeNull() =>
            Assert.IsNotNull(_session.Character.StaticBonusList.Last().DateEnd);

        private void ItemStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(PetBasketVNum));

        private void NotInPairShouldBeSent()
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.NotInPair);
            Assert.IsNotNull(say);
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

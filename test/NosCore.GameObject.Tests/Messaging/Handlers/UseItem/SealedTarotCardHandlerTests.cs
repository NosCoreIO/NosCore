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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
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
    public class SealedTarotCardHandlerTests
    {
        private const short TarotVNum = 1810;
        private const short RewardVNum = 1013;
        private ClientSession _session = null!;
        private IItemGenerationService _itemProvider = null!;
        private SealedTarotCardHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _itemProvider = TestHelpers.Instance.GenerateItemProvider();
            _handler = new SealedTarotCardHandler(_itemProvider);
        }

        [TestMethod]
        public async Task WrongEffectIsIgnored()
        {
            await new Spec("A non-SealedTarotCard effect does not add any reward nor consume the card")
                .Given(TarotInInventoryWithEffectAndValue_, ItemEffectType.Teleport, RewardVNum)
                .WhenAsync(UsingTheItem)
                .Then(TarotStackShouldStillBe_, (short)1)
                .And(InventoryShouldNotContain_, RewardVNum)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ZeroEffectValueIsIgnored()
        {
            await new Spec("EffectValue<=0 is a malformed tarot row: no reward, no consumption")
                .Given(TarotInInventoryWithEffectAndValue_, ItemEffectType.SealedTarotCard, (short)0)
                .WhenAsync(UsingTheItem)
                .Then(TarotStackShouldStillBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidTarotGrantsRewardAndConsumesCard()
        {
            await new Spec("A valid SealedTarotCard grants EffectValue's item once and consumes one card")
                .Given(TarotInInventoryWithEffectAndValue_, ItemEffectType.SealedTarotCard, RewardVNum)
                .WhenAsync(UsingTheItem)
                .Then(InventoryShouldContain_, RewardVNum)
                .And(TarotStackShouldStillBe_, (short)0)
                .And(ItemReceivedShouldBeSent)
                .ExecuteAsync();
        }

        private void TarotInInventoryWithEffectAndValue_(ItemEffectType effect, short value)
        {
            var item = new Item
            {
                VNum = TarotVNum,
                Type = NoscorePocketType.Main,
                Effect = effect,
                EffectValue = value,
            };
            var inst = new ItemInstanceForTest(TarotVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

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

        private void TarotStackShouldStillBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(TarotVNum));

        private void InventoryShouldContain_(short vnum) =>
            Assert.IsTrue(_session.Character.InventoryService.CountItem(vnum) > 0);

        private void InventoryShouldNotContain_(short vnum) =>
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(vnum));

        private void ItemReceivedShouldBeSent()
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.ItemReceived);
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

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
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class SpeakerHandlerTests
    {
        private const short SpeakerVNum = 5792;
        private ClientSession _session = null!;
        private SpeakerHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new SpeakerHandler();
        }

        [TestMethod]
        public async Task NonMagicalItemIsIgnored()
        {
            await new Spec("Non-Magical ItemType is a no-op: no TextInput prompt emitted")
                .Given(ItemWith_, ItemType.Main, ItemEffectType.Speaker)
                .WhenAsync(UsingTheItem)
                .Then(NoGuriTextInputShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WrongEffectIsIgnored()
        {
            await new Spec("Magical item with a non-Speaker Effect does not prompt TextInput")
                .Given(ItemWith_, ItemType.Magical, ItemEffectType.Teleport)
                .WhenAsync(UsingTheItem)
                .Then(NoGuriTextInputShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MagicalSpeakerItemOpensTextInputDialog()
        {
            await new Spec("A Magical + Speaker item opens the TextInput guri dialog bound to the item's inventory slot")
                .Given(ItemWith_, ItemType.Magical, ItemEffectType.Speaker)
                .WhenAsync(UsingTheItem)
                .Then(GuriTextInputShouldBeSentForCurrentSlot)
                .ExecuteAsync();
        }

        private void ItemWith_(ItemType itemType, ItemEffectType effect)
        {
            var item = new Item
            {
                VNum = SpeakerVNum,
                Type = NoscorePocketType.Main,
                ItemType = itemType,
                Effect = effect,
            };
            var inst = new ItemInstanceForTest(SpeakerVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 7;
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
                Slot = 7,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _item, packet));
        }

        private void NoGuriTextInputShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<GuriPacket>()
                .Any(p => p.Type == GuriPacketType.TextInput));

        private void GuriTextInputShouldBeSentForCurrentSlot()
        {
            var guri = _session.LastPackets.OfType<GuriPacket>()
                .LastOrDefault(p => p.Type == GuriPacketType.TextInput);
            Assert.IsNotNull(guri);
            Assert.AreEqual(3u, guri.Argument);
            Assert.AreEqual(1u, guri.SecondArgument);
            Assert.AreEqual(7L, guri.EntityId);
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

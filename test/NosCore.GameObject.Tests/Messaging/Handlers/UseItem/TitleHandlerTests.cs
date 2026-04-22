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
    public class TitleHandlerTests
    {
        private const short TitleVNum = 9054;
        private ClientSession _session = null!;
        private TitleHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new TitleHandler();
        }

        [TestMethod]
        public async Task NonTitleItemIsIgnored()
        {
            await new Spec("Non-Title ItemType does not prompt the Qnai confirmation")
                .Given(ItemOfType_, ItemType.Main)
                .WhenAsync(UsingTheItem)
                .Then(NoQnaiPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TitleItemEmitsAskAddTitleConfirmation()
        {
            await new Spec("A Title-typed item prompts a Qnai with Question=AskAddTitle and a Guri/Title YesPacket")
                .Given(ItemOfType_, ItemType.Title)
                .WhenAsync(UsingTheItem)
                .Then(QnaiWithAskAddTitleAndGuriYesPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void ItemOfType_(ItemType itemType)
        {
            var item = new Item
            {
                VNum = TitleVNum,
                Type = NoscorePocketType.Main,
                ItemType = itemType,
            };
            var inst = new ItemInstanceForTest(TitleVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 4;
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
                Slot = 4,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _item, packet));
        }

        private void NoQnaiPacketShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<QnaiPacket>().Any());

        private void QnaiWithAskAddTitleAndGuriYesPacketShouldBeSent()
        {
            var qnai = _session.LastPackets.OfType<QnaiPacket>().LastOrDefault();
            Assert.IsNotNull(qnai);
            Assert.AreEqual(Game18NConstString.AskAddTitle, qnai.Question);
            var guri = qnai.YesPacket as GuriPacket;
            Assert.IsNotNull(guri);
            Assert.AreEqual(GuriPacketType.Title, guri.Type);
            Assert.AreEqual((uint)TitleVNum, guri.Argument);
            Assert.AreEqual(4L, guri.EntityId);
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

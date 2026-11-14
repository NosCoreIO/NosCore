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
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;
using ServerGuriPacket = NosCore.Packets.ServerPackets.UI.GuriPacket;
using TextReplyHandler = NosCore.GameObject.Messaging.Handlers.Guri.SelfIntroductionHandler;
using UseIntroductionHandler = NosCore.GameObject.Messaging.Handlers.UseItem.SelfIntroductionHandler;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class SelfIntroductionHandlerTests
    {
        private const short IntroductionVNum = 1117;

        private TestWorld World;
        private ClientSession _session;
        private InventoryItemInstance _introduction;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task UsingTheItemOpensTheIntroductionWindow()
        {
            await new Spec("Using a Self-Introduction opens the window to type the introduction")
                .Given(ASelfIntroductionInTheInventory)
                .WhenAsync(UsingTheItem)
                .Then(TheTextWindowShouldOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTypedTextBecomesTheIntroduction()
        {
            await new Spec("The typed text becomes the introduction on the character card and uses up the item")
                .Given(ASelfIntroductionInTheInventory)
                .WhenAsync(Typing_, " Hello there ")
                .Then(TheIntroductionShouldBe_, "Hello there")
                .And(TheCardShouldShow_, "Hello there")
                .And(TheRegistrationShouldBeConfirmed)
                .And(ItemsLeftShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALongTextIsCutAtTheClientsLimit()
        {
            await new Spec("Text past 60 characters is cut at 60")
                .Given(ASelfIntroductionInTheInventory)
                .WhenAsync(Typing_, new string('a', 75))
                .Then(TheIntroductionShouldBe_, new string('a', 60))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutTheItemTheTextIsIgnored()
        {
            await new Spec("Text typed without a Self-Introduction in the inventory changes nothing")
                .WhenAsync(Typing_, "Hello there")
                .Then(NoIntroductionShouldBeSet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEmptyTextKeepsTheItem()
        {
            await new Spec("Confirming the window without text keeps the item")
                .Given(ASelfIntroductionInTheInventory)
                .WhenAsync(Typing_, " ")
                .Then(NoIntroductionShouldBeSet)
                .And(ItemsLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        private void ASelfIntroductionInTheInventory()
        {
            var item = new Item
            {
                VNum = IntroductionVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = ItemEffectType.SelfIntroduction,
            };
            _introduction = InventoryItemInstance.Create(
                new ItemInstanceForTest(IntroductionVNum) { Amount = 1, Item = item }, _session.Character.CharacterId);
            _introduction.Slot = 3;
            _introduction.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_introduction.ItemInstanceId] = _introduction;
        }

        private Task UsingTheItem() =>
            new UseIntroductionHandler().Handle(new ItemUsedEvent(_session, _introduction, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = _introduction.Slot,
            }));

        private Task Typing_(string text) =>
            World.Create<TextReplyHandler>().Handle(new GuriPacketReceivedEvent(_session, new ClientGuriPacket
            {
                Type = GuriPacketType.TextInputReply,
                Argument = (int)TextReplyHandler.TextInputArgument,
                VisualId = 0,
                Data = 0,
                Value = text,
            }));

        private void TheTextWindowShouldOpen() =>
            Assert.IsTrue(_session.LastPackets.OfType<ServerGuriPacket>().Any(g =>
                g.Type == GuriPacketType.TextInput && g.Argument == TextReplyHandler.TextInputArgument));

        private void TheIntroductionShouldBe_(string text) =>
            Assert.AreEqual(text, _session.Character.Biography);

        private void NoIntroductionShouldBeSet() =>
            Assert.IsNull(_session.Character.Biography);

        private void TheCardShouldShow_(string text) =>
            Assert.AreEqual(text, _session.Character.GenerateReqInfo(TestLocalizers.Game, RegionType.EN).Biography);

        private void TheRegistrationShouldBeConfirmed() =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>()
                .Any(m => m.Message == Game18NConstString.SelfIntroductionRegistered));

        private void ItemsLeftShouldBe_(int count) =>
            Assert.AreEqual(count, _session.Character.InventoryService.CountItem(IntroductionVNum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

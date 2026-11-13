//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;
using ServerGuriPacket = NosCore.Packets.ServerPackets.UI.GuriPacket;
using TextReplyHandler = NosCore.GameObject.Messaging.Handlers.Guri.SpeechBubbleHandler;
using UseBubbleHandler = NosCore.GameObject.Messaging.Handlers.UseItem.SpeechBubbleHandler;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class SpeechBubbleHandlerTests
    {
        private const short BubbleVNum = 2174;

        private TestWorld World;
        private ClientSession _session;
        private InventoryItemInstance _bubble;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task UsingTheBubbleOpensItsTextWindow()
        {
            await new Spec("Using a speech bubble opens the window to type its text")
                .Given(ABubbleInTheInventory)
                .WhenAsync(UsingTheBubble)
                .Then(TheTextWindowShouldOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTypedTextBecomesTheBubble()
        {
            await new Spec("The typed text goes over the character for 30 minutes and uses up the bubble")
                .Given(ABubbleInTheInventory)
                .WhenAsync(Typing_, "Hello there")
                .Then(TheClientShouldBeGivenTheBubble_, "Hello there")
                .And(TheBubbleShouldLastUntil_MinutesFromNow, 30)
                .And(BubblesLeftShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutABubbleTheTextIsIgnored()
        {
            await new Spec("Text typed without a bubble in the inventory puts nothing over the character")
                .WhenAsync(Typing_, "Hello there")
                .Then(NoBubbleShouldBeGiven)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEmptyTextKeepsTheBubble()
        {
            await new Spec("Confirming the window without text keeps the bubble")
                .Given(ABubbleInTheInventory)
                .WhenAsync(Typing_, " ")
                .Then(NoBubbleShouldBeGiven)
                .And(BubblesLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        private void ABubbleInTheInventory()
        {
            var item = new Item
            {
                VNum = BubbleVNum,
                Type = NoscorePocketType.Etc,
                ItemType = ItemType.Magical,
                Effect = ItemEffectType.SpeechBubble,
                EffectValue = 30,
            };
            _bubble = InventoryItemInstance.Create(new ItemInstanceForTest(BubbleVNum) { Amount = 1, Item = item },
                _session.Character.CharacterId);
            _bubble.Slot = 7;
            _bubble.Type = NoscorePocketType.Etc;
            _session.Character.InventoryService[_bubble.ItemInstanceId] = _bubble;
        }

        private Task UsingTheBubble() =>
            new UseBubbleHandler().Handle(new ItemUsedEvent(_session, _bubble, new UseItemPacket
            {
                VisualType = NosCore.Shared.Enumerations.VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Etc,
                Slot = _bubble.Slot,
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

        private void TheClientShouldBeGivenTheBubble_(string text)
        {
            Assert.AreEqual(text, _session.LastPackets.OfType<BubbleReplyPacket>().Single().Message);
            Assert.AreEqual(text, _session.Character.SpeechBubble);
        }

        private void TheBubbleShouldLastUntil_MinutesFromNow(int minutes) =>
            Assert.AreEqual(World.Clock.GetCurrentInstant() + Duration.FromMinutes(minutes),
                _session.Character.SpeechBubbleExpiresAt);

        private void NoBubbleShouldBeGiven()
        {
            Assert.IsFalse(_session.LastPackets.OfType<BubbleReplyPacket>().Any());
            Assert.IsNull(_session.Character.SpeechBubble);
        }

        private void BubblesLeftShouldBe_(int count) =>
            Assert.AreEqual(count, _session.Character.InventoryService.CountItem(BubbleVNum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Etc };
            public object Clone() => MemberwiseClone();
        }
    }
}

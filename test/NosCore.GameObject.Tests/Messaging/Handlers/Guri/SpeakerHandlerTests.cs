//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class SpeakerHandlerTests
    {
        private const short SpeakerVNum = 2173;
        private const short SpeakerSlot = 4;
        private ClientSession _speaker;
        private ClientSession _listener;
        private SpeakerHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _speaker = await TestHelpers.Instance.GenerateSessionAsync();
            _listener = await TestHelpers.Instance.GenerateSessionAsync();
            Register(_speaker);
            Register(_listener);
            GiveSpeakerItem();
            _handler = new SpeakerHandler(NullLogger<SpeakerHandler>.Instance, TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.GameLanguageLocalizer, TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task ListenerBlockingHeroChatDoesNotHearTheSpeaker()
        {
            await new Spec("A player who blocks hero chat does not hear the speaker")
                .Given(ListenerBlocksHeroChat)
                .WhenAsync(SpeakerIsUsed)
                .Then(ListenerShouldNotHearIt)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListenerAllowingHeroChatHearsTheSpeaker()
        {
            await new Spec("A player who allows hero chat hears the speaker")
                .Given(ListenerAllowsHeroChat)
                .WhenAsync(SpeakerIsUsed)
                .Then(ListenerShouldHearIt)
                .ExecuteAsync();
        }

        private static void Register(ClientSession session) =>
            TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = session.Channel.NotNull().Id,
                SessionId = session.SessionId,
                Sender = session,
                AccountName = session.Account.Name,
                Disconnect = () => Task.CompletedTask,
                CharacterId = session.Character.CharacterId,
                MapInstanceId = session.Character.MapInstance.MapInstanceId,
            });

        private void GiveSpeakerItem()
        {
            var item = new Item
            {
                VNum = SpeakerVNum,
                Type = NoscorePocketType.Etc,
                ItemType = ItemType.Magical,
                Effect = ItemEffectType.Speaker,
            };
            var instance = new ItemInstanceForTest(SpeakerVNum) { Amount = 1, Item = item };
            var inventoryItem = InventoryItemInstance.Create(instance, _speaker.Character.CharacterId);
            inventoryItem.Slot = SpeakerSlot;
            inventoryItem.Type = NoscorePocketType.Etc;
            _speaker.Character.InventoryService[inventoryItem.ItemInstanceId] = inventoryItem;
        }

        private void ListenerBlocksHeroChat() => _listener.Character.HeroChatBlocked = true;

        private void ListenerAllowsHeroChat() => _listener.Character.HeroChatBlocked = false;

        private Task SpeakerIsUsed() =>
            _handler.Handle(new GuriPacketReceivedEvent(_speaker, new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = SpeakerSlot,
                Data = 0,
                Value = "hello",
            }));

        private void ListenerShouldNotHearIt() =>
            Assert.IsFalse(_listener.LastPackets.OfType<SayPacket>().Any());

        private void ListenerShouldHearIt() =>
            Assert.IsTrue(_listener.LastPackets.OfType<SayPacket>().Any());

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Etc };
            public object Clone() => MemberwiseClone();
        }
    }
}

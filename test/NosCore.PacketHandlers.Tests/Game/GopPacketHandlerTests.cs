//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using NosCore.GameObject.Ecs.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GopPacketHandlerTests
    {
        private GopPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IPubSubHub> PubSubHub = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            PubSubHub = new Mock<IPubSubHub>();
            Handler = new GopPacketHandler(PubSubHub.Object, NullLogger<GopPacketHandler>.Instance);
        }

        [TestMethod]
        public async Task TurningExchangeOffBlocksTrades()
        {
            await new Spec("Turning the exchange option off blocks trades")
                .WhenAsync(SettingOption_To_, GopPacketType.ExchangeBlocked, false)
                .Then(ExchangeShouldBeBlocked_, true)
                .And(MessageShouldBe_, Game18NConstString.TradeBlocked)
                .And(StatOptionBit_ShouldBe_, GopPacketType.ExchangeBlocked, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TurningExchangeOnAllowsTrades()
        {
            await new Spec("Turning the exchange option on allows trades again")
                .Given(ExchangeIsBlocked)
                .WhenAsync(SettingOption_To_, GopPacketType.ExchangeBlocked, true)
                .Then(ExchangeShouldBeBlocked_, false)
                .And(MessageShouldBe_, Game18NConstString.TradeAllowed)
                .And(StatOptionBit_ShouldBe_, GopPacketType.ExchangeBlocked, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TurningGroupOffBlocksGroupRequests()
        {
            await new Spec("Turning the party option off blocks group requests")
                .WhenAsync(SettingOption_To_, GopPacketType.GroupRequestBlocked, false)
                .Then(GroupRequestsShouldBeBlocked)
                .And(MessageShouldBe_, Game18NConstString.GroupRequestBlocked)
                .And(StatOptionBit_ShouldBe_, GopPacketType.GroupRequestBlocked, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TurningWhisperOffTellsTheMasterServer()
        {
            await new Spec("Turning the whisper option off is published to the master server")
                .WhenAsync(SettingOption_To_, GopPacketType.WhisperBlocked, false)
                .Then(MessageShouldBe_, Game18NConstString.WhisperBlocked)
                .And(SubscriberShouldBePublishedWithWhisperBlocked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TurningFriendOffTellsTheMasterServer()
        {
            await new Spec("Turning the friend option off is published to the master server")
                .WhenAsync(SettingOption_To_, GopPacketType.FriendRequestBlocked, false)
                .Then(MessageShouldBe_, Game18NConstString.FriendBlocked)
                .And(SubscriberShouldBePublishedWithFriendRequestBlocked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RejectedMasterUpdateStillAppliesLocally()
        {
            await new Spec("A whisper option the master server rejects still applies on this channel")
                .Given(MasterServerRejectsUpdates)
                .WhenAsync(SettingOption_To_, GopPacketType.WhisperBlocked, false)
                .Then(WhispersShouldBeBlocked)
                .And(MessageShouldBe_, Game18NConstString.WhisperBlocked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlockingEmoticonsIsStored()
        {
            await new Spec("Blocking emoticons is stored and echoed in the stat packet")
                .WhenAsync(SettingOption_To_, GopPacketType.EmoticonsBlocked, true)
                .Then(EmoticonsShouldBeBlocked)
                .And(MessageShouldBe_, Game18NConstString.SmileysDisabled)
                .And(StatOptionBit_ShouldBe_, GopPacketType.EmoticonsBlocked, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlockingHeroChatIsStored()
        {
            await new Spec("Blocking hero chat is stored and echoed in the stat packet")
                .WhenAsync(SettingOption_To_, GopPacketType.HeroChatBlocked, true)
                .Then(HeroChatShouldBeBlocked)
                .And(MessageShouldBe_, Game18NConstString.SpeakerDisabled)
                .And(StatOptionBit_ShouldBe_, GopPacketType.HeroChatBlocked, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LockingMouseAimClearsItsStatBit()
        {
            await new Spec("Locking the mouse aim clears its bit in the stat packet")
                .WhenAsync(SettingOption_To_, GopPacketType.MouseAimLock, true)
                .Then(MouseAimShouldBeLocked)
                .And(StatOptionBit_ShouldBe_, GopPacketType.MouseAimLock, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TurningFamilyOffBlocksFamilyInvites()
        {
            await new Spec("Turning the family option off is stored")
                .WhenAsync(SettingOption_To_, GopPacketType.FamilyRequestBlocked, false)
                .Then(FamilyRequestsShouldBeBlocked)
                .And(MessageShouldBe_, Game18NConstString.FamilyBlocked)
                .And(StatOptionBit_ShouldBe_, GopPacketType.FamilyRequestBlocked, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryOptionIsStoredAndEchoed()
        {
            await new Spec("Every option of the window is stored and echoed in the stat packet")
                .WhenAsync(TogglingEveryOption)
                .Then(EveryOptionShouldBeStoredAndEchoed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnknownOptionIsIgnored()
        {
            await new Spec("An option the client window does not have sends nothing back")
                .WhenAsync(SettingOption_To_, (GopPacketType)99, true)
                .Then(NothingShouldBeSent)
                .ExecuteAsync();
        }

        private async Task TogglingEveryOption()
        {
            foreach (var option in Enum.GetValues<GopPacketType>())
            {
                await SettingOption_To_(option, true);
            }
        }

        private void EveryOptionShouldBeStoredAndEchoed()
        {
            var accepted = new[]
            {
                GopPacketType.ExchangeBlocked, GopPacketType.FriendRequestBlocked, GopPacketType.FamilyRequestBlocked,
                GopPacketType.WhisperBlocked, GopPacketType.GroupRequestBlocked,
                GopPacketType.HatInVisible, GopPacketType.LockInterface
            };
            foreach (var option in Enum.GetValues<GopPacketType>())
            {
                var expected = !accepted.Contains(option);
                Assert.AreEqual(expected, Session.Character.GetOption(option), option.ToString());
                StatOptionBit_ShouldBe_(option, expected != (option == GopPacketType.MouseAimLock));
            }
        }

        private void FamilyRequestsShouldBeBlocked() => Assert.IsTrue(Session.Character.FamilyRequestBlocked);

        private void ExchangeIsBlocked() => Session.Character.ExchangeBlocked = true;

        private void MasterServerRejectsUpdates() =>
            PubSubHub.Setup(x => x.SubscribeAsync(It.IsAny<Subscriber>())).ReturnsAsync(false);

        private void WhispersShouldBeBlocked() => Assert.IsTrue(Session.Character.WhisperBlocked);

        private Task SettingOption_To_(GopPacketType option, bool isActive) =>
            Handler.ExecuteAsync(new GopPacket { Option = option, IsActive = isActive }, Session);

        private void ExchangeShouldBeBlocked_(bool blocked) => Assert.AreEqual(blocked, Session.Character.ExchangeBlocked);

        private void GroupRequestsShouldBeBlocked() => Assert.IsTrue(Session.Character.GroupRequestBlocked);

        private void EmoticonsShouldBeBlocked() => Assert.IsTrue(Session.Character.EmoticonsBlocked);

        private void HeroChatShouldBeBlocked() => Assert.IsTrue(Session.Character.HeroChatBlocked);

        private void MouseAimShouldBeLocked() => Assert.IsTrue(Session.Character.MouseAimLock);

        private void MessageShouldBe_(Game18NConstString message)
        {
            var packet = Session.LastPackets.OfType<MsgiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(message, packet.Message);
        }

        private void StatOptionBit_ShouldBe_(GopPacketType option, bool set)
        {
            var stat = Session.LastPackets.OfType<StatPacket>().LastOrDefault();
            Assert.IsNotNull(stat);
            var bit = 1 << ((int)option - 1);
            Assert.AreEqual(set, ((int)stat.Option & bit) != 0);
        }

        private void SubscriberShouldBePublishedWithWhisperBlocked()
        {
            var characterId = Session.Character.CharacterId;
            PubSubHub.Verify(x => x.SubscribeAsync(It.Is<Subscriber>(s =>
                s.ConnectedCharacter!.Id == characterId && s.ConnectedCharacter.WhisperBlocked)), Times.Once);
        }

        private void SubscriberShouldBePublishedWithFriendRequestBlocked()
        {
            var characterId = Session.Character.CharacterId;
            PubSubHub.Verify(x => x.SubscribeAsync(It.Is<Subscriber>(s =>
                s.ConnectedCharacter!.Id == characterId && s.ConnectedCharacter.FriendRequestBlocked)), Times.Once);
        }

        private void NothingShouldBeSent()
        {
            Assert.IsFalse(Session.LastPackets.OfType<MsgiPacket>().Any());
            Assert.IsFalse(Session.LastPackets.OfType<StatPacket>().Any());
        }
    }
}

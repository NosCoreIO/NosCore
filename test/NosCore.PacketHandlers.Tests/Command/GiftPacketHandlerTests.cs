//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class GiftPacketHandlerTests
    {
        private GiftPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<IMailHub> MailHub = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            PubSubHub = new Mock<IPubSubHub>();
            MailHub = new Mock<IMailHub>();

            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>()));

            Handler = new GiftPacketHandler(
                PubSubHub.Object,
                MailHub.Object,
                TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task SendingGiftToUnknownPlayerShouldShowError()
        {
            await new Spec("Sending gift to unknown player should show error")
                .Given(CharacterIsOnMap)
                .WhenAsync(SendingGiftToUnknownPlayer)
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SendingGiftToOnlinePlayerShouldSendMail()
        {
            await new Spec("Sending gift to online player should send mail")
                .Given(CharacterIsOnMap)
                .And(TargetPlayerIsOnline)
                .WhenAsync(SendingGiftToTargetPlayer)
                .Then(MailShouldBeSent)
                .And(ShouldReceiveGiftDeliveredMessage)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void TargetPlayerIsOnline()
        {
            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>
                {
                    new Subscriber
                    {
                        ConnectedCharacter = new Character
                        {
                            Id = 12345,
                            Name = "TargetPlayer"
                        }
                    }
                }));
        }

        private async Task SendingGiftToUnknownPlayer()
        {
            await Handler.ExecuteAsync(new GiftPacket
            {
                CharacterName = "UnknownPlayer123",
                VNum = 1012,
                Amount = 1,
                Rare = 0,
                Upgrade = 0
            }, Session);
        }

        private async Task SendingGiftToTargetPlayer()
        {
            await Handler.ExecuteAsync(new GiftPacket
            {
                CharacterName = "TargetPlayer",
                VNum = 1012,
                Amount = 10,
                Rare = 0,
                Upgrade = 0
            }, Session);
        }

        private void ShouldReceiveUnknownCharacterMessage()
        {
            var packet = Session.LastPackets.OfType<InfoiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.UnknownCharacter, packet.Message);
        }

        private void MailShouldBeSent()
        {
            MailHub.Verify(x => x.SendMailAsync(It.IsAny<MailRequest>()), Times.Once);
        }

        private void ShouldReceiveGiftDeliveredMessage()
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.GiftDelivered, packet.Message);
        }
    }
}

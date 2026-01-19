//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class KickPacketHandlerTests
    {
        private KickPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IPubSubHub> PubSubHub = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            PubSubHub = new Mock<IPubSubHub>();

            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>()));

            Handler = new KickPacketHandler(PubSubHub.Object);
        }

        [TestMethod]
        public async Task KickingUnknownPlayerShouldShowErrorMessage()
        {
            await new Spec("Kicking unknown player should show error message")
                .Given(CharacterIsOnMap)
                .WhenAsync(KickingUnknownPlayer)
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KickingOnlinePlayerShouldUnsubscribeThem()
        {
            await new Spec("Kicking online player should unsubscribe them")
                .Given(CharacterIsOnMap)
                .And(TargetPlayerIsOnline)
                .WhenAsync(KickingTargetPlayer)
                .Then(TargetShouldBeUnsubscribed)
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

        private async Task KickingUnknownPlayer()
        {
            await Handler.ExecuteAsync(new KickPacket
            {
                Name = "UnknownPlayer123"
            }, Session);
        }

        private async Task KickingTargetPlayer()
        {
            await Handler.ExecuteAsync(new KickPacket
            {
                Name = "TargetPlayer"
            }, Session);
        }

        private void ShouldReceiveUnknownCharacterMessage()
        {
            var packet = Session.LastPackets.OfType<InfoiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.UnknownCharacter, packet.Message);
        }

        private void TargetShouldBeUnsubscribed()
        {
            PubSubHub.Verify(x => x.UnsubscribeAsync(12345), Times.Once);
        }
    }
}

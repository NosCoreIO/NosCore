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
using NosCore.GameObject.InterChannelCommunication.Messages;
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
    public class SetReputationPacketHandlerTests
    {
        private SetReputationPacketHandler Handler = null!;
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

            Handler = new SetReputationPacketHandler(PubSubHub.Object);
        }

        [TestMethod]
        public async Task SettingOwnReputationShouldChangeReputation()
        {
            await new Spec("Setting own reputation should change reputation")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingOwnReputation)
                .Then(ReputationShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingReputationWithEmptyNameShouldChangeOwnReputation()
        {
            await new Spec("Setting reputation with empty name should change own reputation")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingReputationWithEmptyName)
                .Then(ReputationShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingReputationForUnknownPlayerShouldShowError()
        {
            await new Spec("Setting reputation for unknown player should show error")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingReputationForUnknownPlayer)
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingReputationForOnlinePlayerShouldSendStatData()
        {
            await new Spec("Setting reputation for online player should send stat data")
                .Given(CharacterIsOnMap)
                .And(TargetPlayerIsOnline)
                .WhenAsync(SettingReputationForTargetPlayer)
                .Then(StatDataShouldBeSent)
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

        private async Task SettingOwnReputation()
        {
            await Handler.ExecuteAsync(new SetReputationPacket
            {
                Reputation = 100000,
                Name = Session.Character.Name
            }, Session);
        }

        private async Task SettingReputationWithEmptyName()
        {
            await Handler.ExecuteAsync(new SetReputationPacket
            {
                Reputation = 100000,
                Name = ""
            }, Session);
        }

        private async Task SettingReputationForUnknownPlayer()
        {
            await Handler.ExecuteAsync(new SetReputationPacket
            {
                Reputation = 100000,
                Name = "UnknownPlayer123"
            }, Session);
        }

        private async Task SettingReputationForTargetPlayer()
        {
            await Handler.ExecuteAsync(new SetReputationPacket
            {
                Reputation = 100000,
                Name = "TargetPlayer"
            }, Session);
        }

        private void ReputationShouldBeChanged()
        {
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<StatData>()), Times.Never);
        }

        private void ShouldReceiveUnknownCharacterMessage()
        {
            var packet = Session.LastPackets.OfType<InfoiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.UnknownCharacter, packet.Message);
        }

        private void StatDataShouldBeSent()
        {
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<StatData>()), Times.Once);
        }
    }
}

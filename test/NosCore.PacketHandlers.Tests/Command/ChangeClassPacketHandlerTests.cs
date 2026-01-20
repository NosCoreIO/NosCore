//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data.CommandPackets;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class ChangeClassPacketHandlerTests
    {
        private ChangeClassPacketHandler Handler = null!;
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

            Handler = new ChangeClassPacketHandler(PubSubHub.Object,
                TestHelpers.Instance.WorldConfiguration, new ExperienceService(), new JobExperienceService(), new HeroExperienceService());
        }

        [TestMethod]
        public async Task ChangingOwnClassShouldCallChangeClass()
        {
            await new Spec("Changing own class should call ChangeClass")
                .Given(CharacterIsOnMap)
                .WhenAsync(ChangingOwnClass)
                .Then(CharacterClassShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingClassWithEmptyNameShouldChangeOwnClass()
        {
            await new Spec("Changing class with empty name should change own class")
                .Given(CharacterIsOnMap)
                .WhenAsync(ChangingClassWithEmptyName)
                .Then(CharacterClassShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingClassForUnknownPlayerShouldShowError()
        {
            await new Spec("Changing class for unknown player should show error")
                .Given(CharacterIsOnMap)
                .WhenAsync(ChangingClassForUnknownPlayer)
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingClassForOnlinePlayerShouldSendStatData()
        {
            await new Spec("Changing class for online player should send stat data")
                .Given(CharacterIsOnMap)
                .And(TargetPlayerIsOnline)
                .WhenAsync(ChangingClassForTargetPlayer)
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

        private async Task ChangingOwnClass()
        {
            await Handler.ExecuteAsync(new ChangeClassPacket
            {
                ClassType = CharacterClassType.Archer,
                Name = Session.Character.Name
            }, Session);
        }

        private async Task ChangingClassWithEmptyName()
        {
            await Handler.ExecuteAsync(new ChangeClassPacket
            {
                ClassType = CharacterClassType.Swordsman,
                Name = ""
            }, Session);
        }

        private async Task ChangingClassForUnknownPlayer()
        {
            await Handler.ExecuteAsync(new ChangeClassPacket
            {
                ClassType = CharacterClassType.Mage,
                Name = "UnknownPlayer123"
            }, Session);
        }

        private async Task ChangingClassForTargetPlayer()
        {
            await Handler.ExecuteAsync(new ChangeClassPacket
            {
                ClassType = CharacterClassType.Archer,
                Name = "TargetPlayer"
            }, Session);
        }

        private void CharacterClassShouldBeChanged()
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

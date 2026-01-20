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
using NosCore.Core;
using NosCore.Data.CommandPackets;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
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
    public class SetLevelCommandPacketHandlerTests
    {
        private SetLevelCommandPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<IChannelHub> ChannelHub = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            PubSubHub = new Mock<IPubSubHub>();
            ChannelHub = new Mock<IChannelHub>();

            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>()));

            ChannelHub.Setup(x => x.GetCommunicationChannels())
                .Returns(Task.FromResult(new List<ChannelInfo>()));

            Handler = new SetLevelCommandPacketHandler(PubSubHub.Object, ChannelHub.Object,
                new ExperienceService(), new JobExperienceService(), new HeroExperienceService(), TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task SettingOwnLevelShouldChangeLevel()
        {
            await new Spec("Setting own level should change level")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingOwnLevel)
                .Then(LevelShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingLevelWithEmptyNameShouldChangeOwnLevel()
        {
            await new Spec("Setting level with empty name should change own level")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingLevelWithEmptyName)
                .Then(LevelShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingLevelForUnknownPlayerShouldShowError()
        {
            await new Spec("Setting level for unknown player should show error")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingLevelForUnknownPlayer)
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingLevelForOnlinePlayerShouldSendStatData()
        {
            await new Spec("Setting level for online player should send stat data")
                .Given(CharacterIsOnMap)
                .And(TargetPlayerIsOnline)
                .WhenAsync(SettingLevelForTargetPlayer)
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

        private async Task SettingOwnLevel()
        {
            await Handler.ExecuteAsync(new SetLevelCommandPacket
            {
                Level = 99,
                Name = Session.Character.Name
            }, Session);
        }

        private async Task SettingLevelWithEmptyName()
        {
            await Handler.ExecuteAsync(new SetLevelCommandPacket
            {
                Level = 99,
                Name = ""
            }, Session);
        }

        private async Task SettingLevelForUnknownPlayer()
        {
            await Handler.ExecuteAsync(new SetLevelCommandPacket
            {
                Level = 99,
                Name = "UnknownPlayer123"
            }, Session);
        }

        private async Task SettingLevelForTargetPlayer()
        {
            await Handler.ExecuteAsync(new SetLevelCommandPacket
            {
                Level = 99,
                Name = "TargetPlayer"
            }, Session);
        }

        private void LevelShouldBeChanged()
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

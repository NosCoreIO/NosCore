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
    public class SetHeroLevelCommandPacketHandlerTests
    {
        private SetHeroLevelCommandPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private Mock<IExperienceService> ExperienceService = null!;
        private Mock<IJobExperienceService> JobExperienceService = null!;
        private Mock<IHeroExperienceService> HeroExperienceService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            PubSubHub = new Mock<IPubSubHub>();
            ChannelHub = new Mock<IChannelHub>();
            ExperienceService = new Mock<IExperienceService>();
            JobExperienceService = new Mock<IJobExperienceService>();
            HeroExperienceService = new Mock<IHeroExperienceService>();

            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>()));

            ChannelHub.Setup(x => x.GetCommunicationChannels())
                .Returns(Task.FromResult(new List<ChannelInfo>()));

            Handler = new SetHeroLevelCommandPacketHandler(PubSubHub.Object, ChannelHub.Object,
                ExperienceService.Object, JobExperienceService.Object, HeroExperienceService.Object);
        }

        [TestMethod]
        public async Task SettingOwnHeroLevelShouldChangeHeroLevel()
        {
            await new Spec("Setting own hero level should change hero level")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingOwnHeroLevel)
                .Then(HeroLevelShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingHeroLevelWithEmptyNameShouldChangeOwnHeroLevel()
        {
            await new Spec("Setting hero level with empty name should change own hero level")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingHeroLevelWithEmptyName)
                .Then(HeroLevelShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingHeroLevelForUnknownPlayerShouldShowError()
        {
            await new Spec("Setting hero level for unknown player should show error")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingHeroLevelForUnknownPlayer)
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingHeroLevelForOnlinePlayerShouldSendStatData()
        {
            await new Spec("Setting hero level for online player should send stat data")
                .Given(CharacterIsOnMap)
                .And(TargetPlayerIsOnline)
                .WhenAsync(SettingHeroLevelForTargetPlayer)
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

        private async Task SettingOwnHeroLevel()
        {
            await Handler.ExecuteAsync(new SetHeroLevelCommandPacket
            {
                Level = 50,
                Name = Session.Character.Name
            }, Session);
        }

        private async Task SettingHeroLevelWithEmptyName()
        {
            await Handler.ExecuteAsync(new SetHeroLevelCommandPacket
            {
                Level = 50,
                Name = ""
            }, Session);
        }

        private async Task SettingHeroLevelForUnknownPlayer()
        {
            await Handler.ExecuteAsync(new SetHeroLevelCommandPacket
            {
                Level = 50,
                Name = "UnknownPlayer123"
            }, Session);
        }

        private async Task SettingHeroLevelForTargetPlayer()
        {
            await Handler.ExecuteAsync(new SetHeroLevelCommandPacket
            {
                Level = 50,
                Name = "TargetPlayer"
            }, Session);
        }

        private void HeroLevelShouldBeChanged()
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

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
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SetJobLevelCommandPacketHandlerTests
    {
        private SetJobLevelCommandPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
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
            ExperienceService = new Mock<IExperienceService>();
            JobExperienceService = new Mock<IJobExperienceService>();
            HeroExperienceService = new Mock<IHeroExperienceService>();

            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>()));

            Handler = new SetJobLevelCommandPacketHandler(PubSubHub.Object,
                ExperienceService.Object, JobExperienceService.Object, HeroExperienceService.Object);
        }

        [TestMethod]
        public async Task SettingOwnJobLevelShouldChangeJobLevel()
        {
            await new Spec("Setting own job level should change job level")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingOwnJobLevel)
                .Then(JobLevelShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingJobLevelWithEmptyNameShouldChangeOwnJobLevel()
        {
            await new Spec("Setting job level with empty name should change own job level")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingJobLevelWithEmptyName)
                .Then(JobLevelShouldBeChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingJobLevelForUnknownPlayerShouldShowError()
        {
            await new Spec("Setting job level for unknown player should show error")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingJobLevelForUnknownPlayer)
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingJobLevelForOnlinePlayerShouldSendStatData()
        {
            await new Spec("Setting job level for online player should send stat data")
                .Given(CharacterIsOnMap)
                .And(TargetPlayerIsOnline)
                .WhenAsync(SettingJobLevelForTargetPlayer)
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

        private async Task SettingOwnJobLevel()
        {
            await Handler.ExecuteAsync(new SetJobLevelCommandPacket
            {
                Level = 50,
                Name = Session.Character.Name
            }, Session);
        }

        private async Task SettingJobLevelWithEmptyName()
        {
            await Handler.ExecuteAsync(new SetJobLevelCommandPacket
            {
                Level = 50,
                Name = ""
            }, Session);
        }

        private async Task SettingJobLevelForUnknownPlayer()
        {
            await Handler.ExecuteAsync(new SetJobLevelCommandPacket
            {
                Level = 50,
                Name = "UnknownPlayer123"
            }, Session);
        }

        private async Task SettingJobLevelForTargetPlayer()
        {
            await Handler.ExecuteAsync(new SetJobLevelCommandPacket
            {
                Level = 50,
                Name = "TargetPlayer"
            }, Session);
        }

        private void JobLevelShouldBeChanged()
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

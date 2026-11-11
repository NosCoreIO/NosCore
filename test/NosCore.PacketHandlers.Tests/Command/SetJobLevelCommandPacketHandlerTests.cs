//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
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
        private TestWorld World;
        private SetJobLevelCommandPacketHandler Handler;
        private ClientSession Session;
        private Mock<IRemoteCharacterService> RemoteCharacters;
        private Mock<IExperienceService> ExperienceService;
        private Mock<IJobExperienceService> JobExperienceService;
        private Mock<IHeroExperienceService> HeroExperienceService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            RemoteCharacters = World.Freeze<Mock<IRemoteCharacterService>>();
            ExperienceService = World.Freeze<Mock<IExperienceService>>();
            JobExperienceService = World.Freeze<Mock<IJobExperienceService>>();
            HeroExperienceService = World.Freeze<Mock<IHeroExperienceService>>();

            Handler = World.Create<SetJobLevelCommandPacketHandler>();
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
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private void TargetPlayerIsOnline()
        {
            RemoteCharacters.Setup(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateJobLevel, It.IsAny<long>()))
                .ReturnsAsync(true);
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
            RemoteCharacters.Verify(x => x.UpdateStatAsync(It.IsAny<string>(), It.IsAny<UpdateStatActionType>(), It.IsAny<long>()), Times.Never);
        }

        private void ShouldReceiveUnknownCharacterMessage()
        {
            var packet = Session.LastPackets.OfType<InfoiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.UnknownCharacter, packet.Message);
        }

        private void StatDataShouldBeSent()
        {
            RemoteCharacters.Verify(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateJobLevel, It.IsAny<long>()), Times.Once);
        }
    }
}

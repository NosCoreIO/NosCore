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
        private ChangeClassPacketHandler Handler;
        private ClientSession Session;
        private Mock<IRemoteCharacterService> RemoteCharacters;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            RemoteCharacters = new Mock<IRemoteCharacterService>();

            Handler = new ChangeClassPacketHandler(RemoteCharacters.Object,
                TestHelpers.Instance.WorldConfiguration, new ExperienceService(), new JobExperienceService(), new HeroExperienceService(),
                TestHelpers.Instance.GenerateItemProvider(),
                new Mock<NosCore.GameObject.Services.SkillService.ISkillService>().Object);
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
            Session.Character.MapInstance = TestHelpers.Instance.GetBaseMap(1);
        }

        private void TargetPlayerIsOnline()
        {
            RemoteCharacters.Setup(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateClass, It.IsAny<long>()))
                .ReturnsAsync(true);
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
            RemoteCharacters.Verify(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateClass, It.IsAny<long>()), Times.Once);
        }
    }
}

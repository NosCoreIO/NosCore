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
using NosCore.Core;
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
    public class SetLevelCommandPacketHandlerTests
    {
        private TestWorld World;
        private SetLevelCommandPacketHandler Handler;
        private ClientSession Session;
        private Mock<IRemoteCharacterService> RemoteCharacters;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            RemoteCharacters = World.Freeze<Mock<IRemoteCharacterService>>();

            Handler = World.Create<SetLevelCommandPacketHandler>();
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
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private void TargetPlayerIsOnline()
        {
            RemoteCharacters.Setup(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateLevel, It.IsAny<long>()))
                .ReturnsAsync(true);
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
            RemoteCharacters.Verify(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateLevel, It.IsAny<long>()), Times.Once);
        }
    }
}

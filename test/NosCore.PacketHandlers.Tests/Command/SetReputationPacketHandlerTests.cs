//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
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
    public class SetReputationPacketHandlerTests
    {
        private TestWorld World;
        private SetReputationPacketHandler Handler;
        private ClientSession Session;
        private Mock<IRemoteCharacterService> RemoteCharacters;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            RemoteCharacters = World.Freeze<Mock<IRemoteCharacterService>>();

            Handler = World.Create<SetReputationPacketHandler>();
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
        public async Task RaisingOwnReputationShouldAnnounceTheGain()
        {
            await new Spec("Raising own reputation should announce the gain")
                .Given(CharacterIsOnMap)
                .And(CharacterHasReputation_, 40000L)
                .WhenAsync(SettingOwnReputation)
                .Then(ShouldReceiveReputationMessage_And_, Game18NConstString.ReputationIncreased, 60000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoweringOwnReputationShouldAnnounceTheLoss()
        {
            await new Spec("Lowering own reputation should announce the loss")
                .Given(CharacterIsOnMap)
                .And(CharacterHasReputation_, 150000L)
                .WhenAsync(SettingOwnReputation)
                .Then(ShouldReceiveReputationMessage_And_, Game18NConstString.ReputationReduced, 50000L)
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
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private void CharacterHasReputation_(long reputation)
        {
            Session.Character.Reputation = reputation;
        }

        private void TargetPlayerIsOnline()
        {
            RemoteCharacters.Setup(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateReputation, It.IsAny<long>()))
                .ReturnsAsync(true);
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
            RemoteCharacters.Verify(x => x.UpdateStatAsync(It.IsAny<string>(), It.IsAny<UpdateStatActionType>(), It.IsAny<long>()), Times.Never);
        }

        private void ShouldReceiveUnknownCharacterMessage()
        {
            var packet = Session.LastPackets.OfType<InfoiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.UnknownCharacter, packet.Message);
        }

        private void ShouldReceiveReputationMessage_And_(Game18NConstString message, long amount)
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().Single(p => p.Message == message);
            Assert.AreEqual(amount, packet.Game18NArguments[0]);
            Assert.AreEqual(100000L, Session.Character.Reputation);
        }

        private void StatDataShouldBeSent()
        {
            RemoteCharacters.Verify(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateReputation, It.IsAny<long>()), Times.Once);
        }
    }
}

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
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SetGoldCommandPacketHandlerTests
    {
        private TestWorld World;
        private SetGoldCommandPacketHandler Handler;
        private ClientSession Session;
        private Mock<IRemoteCharacterService> RemoteCharacters;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            RemoteCharacters = World.Freeze<Mock<IRemoteCharacterService>>();

            Handler = World.Create<SetGoldCommandPacketHandler>();
        }

        [TestMethod]
        public async Task SettingGoldForUnknownPlayerShouldShowError()
        {
            await new Spec("Setting gold for unknown player should show error")
                .Given(CharacterIsOnMap)
                .WhenAsync(SettingGoldForUnknownPlayer)
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingGoldForOnlinePlayerShouldSendStatData()
        {
            await new Spec("Setting gold for online player should send stat data")
                .Given(CharacterIsOnMap)
                .And(TargetPlayerIsOnline)
                .WhenAsync(SettingGoldForTargetPlayer)
                .Then(StatDataShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private void TargetPlayerIsOnline()
        {
            RemoteCharacters.Setup(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateGold, It.IsAny<long>()))
                .ReturnsAsync(true);
        }

        private async Task SettingGoldForUnknownPlayer()
        {
            await Handler.ExecuteAsync(new SetGoldCommandPacket
            {
                Name = "UnknownPlayer123",
                Gold = 1000000
            }, Session);
        }

        private async Task SettingGoldForTargetPlayer()
        {
            await Handler.ExecuteAsync(new SetGoldCommandPacket
            {
                Name = "TargetPlayer",
                Gold = 1000000
            }, Session);
        }

        private void ShouldReceiveUnknownCharacterMessage()
        {
            var packet = Session.LastPackets.OfType<InfoiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.UnknownCharacter, packet.Message);
        }

        private void StatDataShouldBeSent()
        {
            RemoteCharacters.Verify(x => x.UpdateStatAsync("TargetPlayer", UpdateStatActionType.UpdateGold, It.IsAny<long>()), Times.Once);
        }
    }
}

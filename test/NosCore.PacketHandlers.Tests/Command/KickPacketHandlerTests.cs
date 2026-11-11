//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class KickPacketHandlerTests
    {
        private TestWorld World;
        private KickPacketHandler Handler;
        private ClientSession Session;
        private Mock<IRemoteCharacterService> RemoteCharacters;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            RemoteCharacters = World.Freeze<Mock<IRemoteCharacterService>>();
            Handler = World.Create<KickPacketHandler>();
        }

        [TestMethod]
        public async Task KickingUnknownPlayerShouldShowErrorMessage()
        {
            await new Spec("Kicking unknown player should show error message")
                .WhenAsync(Kicking_, "UnknownPlayer123")
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KickingOnlinePlayerShouldKickThem()
        {
            await new Spec("Kicking online player should kick them and send nothing back")
                .Given(TargetPlayerIsOnline)
                .WhenAsync(Kicking_, "TargetPlayer")
                .Then(TargetShouldBeKicked)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void TargetPlayerIsOnline() =>
            RemoteCharacters.Setup(x => x.KickAsync("TargetPlayer")).ReturnsAsync(true);

        private Task Kicking_(string name) => Handler.ExecuteAsync(new KickPacket { Name = name }, Session);

        private void ShouldReceiveUnknownCharacterMessage() =>
            Assert.AreEqual(Game18NConstString.UnknownCharacter, Session.LastPackets.OfType<InfoiPacket>().Single().Message);

        private void TargetShouldBeKicked() => RemoteCharacters.Verify(x => x.KickAsync("TargetPlayer"), Times.Once);

        private void NoPacketShouldBeSent() => Assert.AreEqual(0, Session.LastPackets.Count);
    }
}

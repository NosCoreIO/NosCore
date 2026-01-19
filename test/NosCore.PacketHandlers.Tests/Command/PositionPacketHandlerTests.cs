//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class PositionPacketHandlerTests
    {
        private PositionPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new PositionPacketHandler();
        }

        [TestMethod]
        public async Task PositionShouldSendSayPacket()
        {
            await new Spec("Position should send say packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingPositionCommand)
                .Then(ShouldReceiveSayPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PositionShouldContainMapId()
        {
            await new Spec("Position should contain map id")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingPositionCommand)
                .Then(SayPacketShouldContainMapInfo)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.Character.PositionX = 0;
            Session.Character.PositionY = 0;
            Session.LastPackets.Clear();
        }

        private async Task ExecutingPositionCommand()
        {
            await Handler.ExecuteAsync(new PositionPacket(), Session);
        }

        private void ShouldReceiveSayPacket()
        {
            var sayPacket = Session.LastPackets.OfType<SayPacket>().FirstOrDefault();
            Assert.IsNotNull(sayPacket);
        }

        private void SayPacketShouldContainMapInfo()
        {
            var sayPacket = Session.LastPackets.OfType<SayPacket>().FirstOrDefault();
            Assert.IsNotNull(sayPacket);
            Assert.IsTrue(sayPacket.Message?.Contains("Map:") ?? false);
            Assert.IsTrue(sayPacket.Message?.Contains("X:0") ?? false);
            Assert.IsTrue(sayPacket.Message?.Contains("Y:0") ?? false);
        }
    }
}

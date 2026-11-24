//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class EscapePacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IInstanceExitService> Service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Service = World.Freeze<Mock<IInstanceExitService>>();
        }

        [TestMethod]
        public async Task EscapingLeavesTheInstance()
        {
            await new Spec("Escaping leaves the instance")
                .WhenAsync(SendingThePacket)
                .Then(TheServiceIsCalled)
                .ExecuteAsync();
        }

        private Task SendingThePacket() =>
            World.Create<EscapePacketHandler>().ExecuteAsync(new EscapePacket(), Session);

        private void TheServiceIsCalled() => Service.Verify(s => s.LeaveAsync(Session), Times.Once);
    }
}

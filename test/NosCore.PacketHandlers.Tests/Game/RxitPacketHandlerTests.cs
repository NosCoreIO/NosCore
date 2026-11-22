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
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class RxitPacketHandlerTests
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
        public async Task LeavingThroughTheExitDialogLeavesTheInstance()
        {
            await new Spec("Leaving through the exit dialog leaves the instance")
                .WhenAsync(SendingThePacket)
                .Then(TheServiceIsCalled)
                .ExecuteAsync();
        }

        private Task SendingThePacket() =>
            World.Create<RxitPacketHandler>().ExecuteAsync(new RxitPacket { State = RxitPacketType.LeaveInstance }, Session);

        private void TheServiceIsCalled() => Service.Verify(s => s.LeaveAsync(Session), Times.Once);
    }
}

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
using NosCore.Packets.ClientPackets.Event;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class RselPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<ITimeSpaceRewardService> Service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Service = World.Freeze<Mock<ITimeSpaceRewardService>>();
        }

        [TestMethod]
        public async Task ClosingTheScoreWindowOpensTheRewardChest()
        {
            await new Spec("Closing the score window opens the reward chest")
                .WhenAsync(SendingThePacket)
                .Then(TheServiceIsCalled)
                .ExecuteAsync();
        }

        private Task SendingThePacket() =>
            World.Create<RselPacketHandler>().ExecuteAsync(new RselPacket(), Session);

        private void TheServiceIsCalled() => Service.Verify(s => s.OpenChestAsync(Session), Times.Once);
    }
}

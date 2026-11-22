//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RaidService;
using NosCore.PacketHandlers.Raid;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Raid
{
    [TestClass]
    public class MkRaidPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IRaidService> Raids;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Raids = World.Freeze<Mock<IRaidService>>();
        }

        [TestMethod]
        public async Task LaunchingTheRaidStartsItForTheLeader()
        {
            await new Spec("Launching the raid starts it for the leader")
                .WhenAsync(Launching)
                .Then(TheRaidIsStarted)
                .ExecuteAsync();
        }

        private Task Launching() => World.Create<MkRaidPacketHandler>().ExecuteAsync(new MkRaidPacket(), Session);

        private void TheRaidIsStarted() => Raids.Verify(s => s.StartAsync(Session), Times.Once);
    }
}

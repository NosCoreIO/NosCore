//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.TalentArenaService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ServerPackets.Arena;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class TaCallPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<ITalentArenaService> Service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Service = World.Freeze<Mock<ITalentArenaService>>();
        }

        [TestMethod]
        public async Task CallingATeammatePassesItsIndex()
        {
            await new Spec("Calling a teammate passes its index")
                .WhenAsync(SendingThePacket)
                .Then(TheServiceIsCalled)
                .ExecuteAsync();
        }

        private Task SendingThePacket() =>
            World.Create<TaCallPacketHandler>().ExecuteAsync(new TaCallPacket { CalledIndex = 2 }, Session);

        private void TheServiceIsCalled() => Service.Verify(s => s.CallAsync(Session, 2), Times.Once);
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.Act4ShipService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class ShipGlacernonHandlersTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IAct4ShipService> _ship;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _ship = World.Freeze<Mock<IAct4ShipService>>();
        }

        [TestMethod]
        public async Task BoardingAsksTheShipToTakeThePlayer()
        {
            var handler = World.Create<GoToShipGlacernonHandler>();

            await handler.HandleAsync(_session, null, new NrunPacket { Runner = NrunRunnerType.GoToShipGlacernon });

            Assert.AreEqual(NrunRunnerType.GoToShipGlacernon, handler.Runner);
            Assert.IsTrue(handler.RequiresNpc);
            _ship.Verify(s => s.BoardAsync(_session), Times.Once);
            _ship.Verify(s => s.DisembarkAsync(It.IsAny<ClientSession>()), Times.Never);
        }

        [TestMethod]
        public async Task CancellingLetsThePlayerOffTheShip()
        {
            var handler = World.Create<CancelShipTravelGlacernonHandler>();

            await handler.HandleAsync(_session, null, new NrunPacket { Runner = NrunRunnerType.CancelShipTravelGlacernon });

            Assert.AreEqual(NrunRunnerType.CancelShipTravelGlacernon, handler.Runner);
            Assert.IsTrue(handler.RequiresNpc);
            _ship.Verify(s => s.DisembarkAsync(_session), Times.Once);
            _ship.Verify(s => s.BoardAsync(It.IsAny<ClientSession>()), Times.Never);
        }
    }
}

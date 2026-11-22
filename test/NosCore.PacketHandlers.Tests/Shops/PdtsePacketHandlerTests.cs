//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ProductionService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class PdtsePacketHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IProductionService> _production;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _production = World.Freeze<Mock<IProductionService>>();
        }

        private Task SendingAsync(PdtsePacketType type) =>
            World.Create<PdtsePacketHandler>().ExecuteAsync(new PdtsePacket { Type = type, VNum = 1003 }, _session);

        [TestMethod]
        public async Task ProducingAnItemAsksTheProductionService()
        {
            await SendingAsync(PdtsePacketType.Produce);

            _production.Verify(s => s.ProduceAsync(_session, 1003), Times.Once);
            _production.Verify(s => s.ShowRecipeAsync(It.IsAny<ClientSession>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task ChoosingAnItemShowsItsRecipe()
        {
            await SendingAsync(PdtsePacketType.ShowRecipe);

            _production.Verify(s => s.ShowRecipeAsync(_session, 1003), Times.Once);
            _production.Verify(s => s.ProduceAsync(It.IsAny<ClientSession>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AnUnknownTypeDoesNothing()
        {
            await SendingAsync((PdtsePacketType)99);

            _production.VerifyNoOtherCalls();
        }
    }
}

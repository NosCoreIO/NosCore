//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MovementService;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class WalkPacketHandlerTests
    {
        private TestWorld World;
        private WalkPacketHandler Handler;
        private ClientSession Session;
        private Mock<IMovementService> MovementService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            Session.Character.PositionX = 4;
            Session.Character.PositionY = 7;
            MovementService = World.Freeze<Mock<IMovementService>>();
            Handler = World.Create<WalkPacketHandler>();
        }

        [TestMethod]
        public async Task ThePacketIsForwardedToTheMovementService()
        {
            await new Spec("The walk packet's destination, speed and checksum are forwarded to the movement service")
                .Given(TheWalkWillBe_, WalkResult.Accepted)
                .WhenAsync(Walking)
                .Then(TheMovementServiceShouldReceiveThePacketValues)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARefusedWalkPutsTheClientBack()
        {
            await new Spec("A refused walk teleports the client back to the server position")
                .Given(TheWalkWillBe_, WalkResult.Refused)
                .WhenAsync(Walking)
                .Then(TheClientShouldBePutBackAt_, (short)4, (short)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARejectedWalkDisconnectsTheClient()
        {
            await new Spec("A rejected walk disconnects the client without a teleport")
                .Given(TheWalkWillBe_, WalkResult.Rejected)
                .WhenAsync(Walking)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void TheWalkWillBe_(WalkResult result)
        {
            MovementService.Setup(x => x.WalkAsync(It.IsAny<ClientSession>(), It.IsAny<short>(), It.IsAny<short>(),
                    It.IsAny<short>(), It.IsAny<byte>()))
                .ReturnsAsync(result);
        }

        private Task Walking()
        {
            return Handler.ExecuteAsync(new WalkPacket
            {
                XCoordinate = 5,
                YCoordinate = 6,
                Speed = 11,
                CheckSum = 1
            }, Session);
        }

        private void TheMovementServiceShouldReceiveThePacketValues()
        {
            MovementService.Verify(x => x.WalkAsync(Session, 5, 6, 11, 1), Times.Once);
        }

        private void TheClientShouldBePutBackAt_(short x, short y)
        {
            var tp = Session.LastPackets.OfType<TpPacket>().Single();
            Assert.AreEqual((x, y), (tp.X, tp.Y));
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

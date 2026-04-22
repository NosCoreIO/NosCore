//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.PathFinder.Interfaces;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class WalkPacketHandlerTests
    {
        private WalkPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IHeuristic> DistanceCalculator = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            DistanceCalculator = new Mock<IHeuristic>();
            DistanceCalculator.Setup(x => x.GetDistance(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns(1);

            Handler = new WalkPacketHandler(
                DistanceCalculator.Object,
                Logger,
                TestHelpers.Instance.Clock,
                TestHelpers.Instance.LogLanguageLocalizer,
                new Mock<Wolverine.IMessageBus>().Object);
        }

        [TestMethod]
        public async Task WalkWithHighSpeedShouldBeIgnored()
        {
            await new Spec("Walk with high speed should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(WalkingWithHighSpeed)
                .Then(PositionShouldNotBeUpdated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WalkShouldExecuteWithoutError()
        {
            await new Spec("Walk should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(WalkingToValidPosition)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WalkOnBaseMapUpdatesMapXY()
        {
            await new Spec("Walk on a BaseMap updates the persistent MapX/Y alongside PositionX/Y")
                .Given(CharacterIsOnMap)
                .WhenAsync(WalkingToValidPosition)
                .Then(MapXYShouldBe_, (short)5, (short)5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WalkInsideMinilandDoesNotUpdateMapXY()
        {
            await new Spec("Walk inside a non-base instance leaves MapX/Y pinned to last base position")
                .Given(CharacterIsOnMap)
                .And(MapInstanceBecomesNormalInstance)
                .And(MapXYAreAlreadyPersistedAt_, (short)48, (short)132)
                .WhenAsync(WalkingToValidPosition)
                .Then(MapXYShouldBe_, (short)48, (short)132)
                .ExecuteAsync();
        }

        private short InitialX;
        private short InitialY;

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.Character.PositionX = 4;
            Session.Character.PositionY = 4;
            Session.Character.Speed = 20;
            InitialX = Session.Character.PositionX;
            InitialY = Session.Character.PositionY;
        }

        private async Task WalkingToValidPosition()
        {
            var checksum = ((5 + 5) % 3) % 2;
            await Handler.ExecuteAsync(new WalkPacket
            {
                XCoordinate = 5,
                YCoordinate = 5,
                Speed = 20,
                CheckSum = (byte)checksum
            }, Session);
        }

        private async Task WalkingWithHighSpeed()
        {
            await Handler.ExecuteAsync(new WalkPacket
            {
                XCoordinate = 5,
                YCoordinate = 5,
                Speed = 100,
                CheckSum = 0
            }, Session);
        }

        private async Task WalkingWithInvalidChecksum()
        {
            await Handler.ExecuteAsync(new WalkPacket
            {
                XCoordinate = 5,
                YCoordinate = 5,
                Speed = 20,
                CheckSum = 99
            }, Session);
        }

        private void PositionShouldBeUpdated()
        {
            Assert.AreEqual(5, Session.Character.PositionX);
            Assert.AreEqual(5, Session.Character.PositionY);
        }

        private void MapInstanceBecomesNormalInstance()
        {
            Session.Character.MapInstance.MapInstanceType = MapInstanceType.NormalInstance;
        }

        private void MapXYAreAlreadyPersistedAt_(short x, short y)
        {
            Session.Character.MapX = x;
            Session.Character.MapY = y;
        }

        private void MapXYShouldBe_(short expectedX, short expectedY)
        {
            Assert.AreEqual(expectedX, Session.Character.MapX);
            Assert.AreEqual(expectedY, Session.Character.MapY);
        }

        private void PositionShouldNotBeUpdated()
        {
            Assert.AreEqual(InitialX, Session.Character.PositionX);
            Assert.AreEqual(InitialY, Session.Character.PositionY);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

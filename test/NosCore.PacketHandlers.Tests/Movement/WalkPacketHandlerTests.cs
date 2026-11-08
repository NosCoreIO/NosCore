//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MovementService;
using NosCore.Packets.ServerPackets.Entities;
using NodaTime;
using System.Linq;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class WalkPacketHandlerTests
    {
        private WalkPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private readonly ILogger<WalkPacketHandler> Logger = new Mock<ILogger<WalkPacketHandler>>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new WalkPacketHandler(
                TestHelpers.Instance.DistanceCalculator,
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                new Mock<Wolverine.IMessageBus>().Object,
                new WalkService(TestHelpers.Instance.Clock));
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

        [TestMethod]
        public async Task PacketsFasterThanTheCharacterCanWalkAreRefused()
        {
            await new Spec("A second walk packet arriving before the first stretch could have been walked is refused")
                .Given(CharacterIsAt_WithSpeed_, (short)0, (short)0, (byte)4)
                .WhenAsync(WalkingTo_, (short)3, (short)0)
                .AndAsync(WalkingTo_, (short)6, (short)0)
                .Then(CharacterShouldBeAt_, (short)3, (short)0)
                .And(ClientShouldBePutBackAt_, (short)3, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketsAtWalkingPaceAreAccepted()
        {
            await new Spec("Walk packets spaced by the time each stretch takes at the character's speed are all accepted")
                .Given(CharacterIsAt_WithSpeed_, (short)0, (short)0, (byte)4)
                .WhenAsync(WalkingTo_, (short)3, (short)0)
                .AndAsync(WalkingTo_After_Milliseconds, (short)6, (short)0, 1875)
                .AndAsync(WalkingTo_After_Milliseconds, (short)3, (short)0, 1875)
                .AndAsync(WalkingTo_After_Milliseconds, (short)0, (short)0, 1875)
                .AndAsync(WalkingTo_After_Milliseconds, (short)3, (short)0, 1875)
                .Then(CharacterShouldBeAt_, (short)3, (short)0)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketsSlightlyAheadOfWalkingPaceAreAccepted()
        {
            await new Spec("A client whose packets come a fifth early, as with a fast clock or bunched traffic, keeps walking")
                .Given(CharacterIsAt_WithSpeed_, (short)0, (short)0, (byte)4)
                .WhenAsync(WalkingTo_, (short)3, (short)0)
                .AndAsync(WalkingTo_After_Milliseconds, (short)6, (short)0, 1500)
                .AndAsync(WalkingTo_After_Milliseconds, (short)3, (short)0, 1500)
                .AndAsync(WalkingTo_After_Milliseconds, (short)0, (short)0, 1500)
                .AndAsync(WalkingTo_After_Milliseconds, (short)3, (short)0, 1500)
                .Then(CharacterShouldBeAt_, (short)3, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMountedCharacterWalksAtTheMountSpeed()
        {
            await new Spec("A character on a vehicle covers the longer stretches its speed allows at that pace")
                .Given(CharacterIsAt_WithSpeed_, (short)0, (short)0, (byte)20)
                .WhenAsync(WalkingTo_, (short)7, (short)0)
                .AndAsync(WalkingTo_After_Milliseconds, (short)0, (short)7, 875)
                .AndAsync(WalkingTo_After_Milliseconds, (short)7, (short)7, 875)
                .AndAsync(WalkingTo_After_Milliseconds, (short)0, (short)0, 875)
                .Then(CharacterShouldBeAt_, (short)0, (short)0)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASpeedChangeMidWalkKeepsTheWalkGoing()
        {
            await new Spec("A speed buff gained and then lost while walking does not cut the walk short")
                .Given(CharacterIsAt_WithSpeed_, (short)0, (short)0, (byte)10)
                .WhenAsync(WalkingTo_, (short)5, (short)0)
                .And(SpeedBecomes_, (byte)16)
                .AndAsync(WalkingTo_After_Milliseconds, (short)0, (short)6, 1250)
                .AndAsync(WalkingTo_After_Milliseconds, (short)7, (short)6, 1100)
                .And(SpeedBecomes_, (byte)10)
                .AndAsync(WalkingTo_After_Milliseconds, (short)2, (short)6, 1100)
                .AndAsync(WalkingTo_After_Milliseconds, (short)0, (short)4, 1250)
                .Then(CharacterShouldBeAt_, (short)0, (short)4)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WalkingThroughAWallIsRefused()
        {
            await new Spec("A destination on the far side of a wall with no way round is refused")
                .Given(CharacterIsAt_WithSpeed_, (short)3, (short)2, (byte)10)
                .And(AWallRunsDownColumn_, (short)4)
                .WhenAsync(WalkingTo_, (short)5, (short)2)
                .Then(CharacterShouldBeAt_, (short)3, (short)2)
                .And(ClientShouldBePutBackAt_, (short)3, (short)2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WalkingRoundACornerIsAccepted()
        {
            await new Spec("A destination reached by stepping round the corner of an obstacle is accepted")
                .Given(CharacterIsAt_WithSpeed_, (short)0, (short)2, (byte)10)
                .WhenAsync(WalkingTo_, (short)4, (short)2)
                .Then(CharacterShouldBeAt_, (short)4, (short)2)
                .ExecuteAsync();
        }

        private void CharacterIsAt_WithSpeed_(short x, short y, byte speed)
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.Character.PositionX = x;
            Session.Character.PositionY = y;
            Session.Character.Speed = speed;
        }

        private void SpeedBecomes_(byte speed)
        {
            Session.Character.Speed = speed;
        }

        private void AWallRunsDownColumn_(short x)
        {
            var map = Session.Character.MapInstance.Map;
            for (short y = 0; y < map.Height; y++)
            {
                map.Data[4 + y * map.Width + x] = 1;
            }
        }

        private Task WalkingTo_(short x, short y)
        {
            return Handler.ExecuteAsync(new WalkPacket
            {
                XCoordinate = x,
                YCoordinate = y,
                Speed = Session.Character.Speed,
                CheckSum = (byte)((x + y) % 3 % 2)
            }, Session);
        }

        private Task WalkingTo_After_Milliseconds(short x, short y, int milliseconds)
        {
            TestHelpers.Instance.Clock.AdvanceMilliseconds(milliseconds);
            return WalkingTo_(x, y);
        }

        private void CharacterShouldBeAt_(short x, short y)
        {
            Assert.AreEqual((x, y), (Session.Character.PositionX, Session.Character.PositionY));
        }

        private void ClientShouldBePutBackAt_(short x, short y)
        {
            var tp = Session.LastPackets.OfType<TpPacket>().LastOrDefault();
            Assert.IsNotNull(tp);
            Assert.AreEqual((x, y), (tp.X, tp.Y));
        }

        [TestMethod]
        public async Task WalkingInterruptsABarInProgress()
        {
            await new Spec("A step taken during a delay bar cancels it")
                .Given(CharacterIsOnMap)
                .AndAsync(ADelayBarIsRunning)
                .WhenAsync(WalkingToValidPosition)
                .Then(TheBarShouldNotCompleteAfterFiveSeconds)
                .ExecuteAsync();
        }

        private static readonly System.Guid BarTarget = System.Guid.NewGuid();

        private Task ADelayBarIsRunning()
        {
            return Session.Character.StartDelayAsync(new NosCore.Packets.ServerPackets.UI.DelayPacket
            {
                Delay = 5000,
                Type = NosCore.Packets.Enumerations.DelayPacketType.ItemInUse,
            }, BarTarget, TestHelpers.Instance.Clock.GetCurrentInstant());
        }

        private void TheBarShouldNotCompleteAfterFiveSeconds()
        {
            TestHelpers.Instance.Clock.AdvanceSeconds(5);
            Assert.IsFalse(Session.Character.TryCompleteDelay(NosCore.Packets.Enumerations.DelayPacketType.ItemInUse,
                BarTarget, TestHelpers.Instance.Clock.GetCurrentInstant()));
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

        private void PositionShouldNotBeUpdated()
        {
            Assert.AreEqual(InitialX, Session.Character.PositionX);
            Assert.AreEqual(InitialY, Session.Character.PositionY);
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

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

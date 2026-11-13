//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Services.BattleService;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.MovementService;
using NosCore.Packets.Enumerations;
using NosCore.PathFinder.Interfaces;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MovementService
{
    [TestClass]
    public class MovementServiceTests
    {
        private TestWorld World;
        private GameObject.Services.MovementService.MovementService Service;
        private ClientSession Session;
        private Mock<IHeuristic> DistanceCalculator;
        private Mock<IMinilandService> MinilandService;
        private Mock<IMapChangeService> MapChangeService;
        private WalkResult LastWalk;
        private PortalResult LastPortal;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            DistanceCalculator = World.Freeze<Mock<IHeuristic>>();
            MinilandService = World.Freeze<Mock<IMinilandService>>();
            MapChangeService = World.Freeze<Mock<IMapChangeService>>();

            DistanceCalculator.Setup(x => x.GetDistance(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns<(short X, short Y), (short X, short Y)>((from, to) =>
                    World.DistanceCalculator.GetDistance(from, to));

            MinilandService.Setup(x => x.GetMinilandPortals(It.IsAny<long>()))
                .Returns(new List<NosCore.GameObject.Map.Portal>());

            Service = World.UseReal<IWalkService, WalkService>().Create<GameObject.Services.MovementService.MovementService>();
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
        public async Task ARootedCharacterCannotWalk()
        {
            await new Spec("A character under a card that stops movement cannot walk")
                .Given(CharacterIsOnMap)
                .And(CharacterIsRooted)
                .WhenAsync(WalkingToValidPosition)
                .Then(PositionShouldNotBeUpdated)
                .And(TheLastWalkShouldBeRefused)
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
        public async Task WalkingTurnsTheCharacterTowardsWhereItWent()
        {
            await new Spec("A walk turns the character the way it went")
                .Given(CharacterIsOnMap)
                .WhenAsync(WalkingToValidPosition)
                .Then(DirectionShouldBe_, (byte)6)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWalkEndingWithAStepBackStillFacesTheWayItWent()
        {
            await new Spec("A walk whose last packet steps back a cell keeps facing the way it went")
                .Given(CharacterIsOnMap)
                .WhenAsync(WalkingTo_After_Milliseconds, (short)7, (short)4, 2000)
                .AndAsync(WalkingTo_After_Milliseconds, (short)6, (short)4, 500)
                .Then(CharacterShouldBeAt_, (short)6, (short)4)
                .And(DirectionShouldBe_, (byte)1)
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
                .And(TheLastWalkShouldBeRefused)
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
                .And(TheLastWalkShouldBeRefused)
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
            Session.Character.MapInstance = World.GetBaseMap(1);
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
            return WalkAsync(x, y, Session.Character.Speed, (byte)((x + y) % 3 % 2));
        }

        private Task WalkingTo_After_Milliseconds(short x, short y, int milliseconds)
        {
            World.Clock.AdvanceMilliseconds(milliseconds);
            return WalkingTo_(x, y);
        }

        private void CharacterShouldBeAt_(short x, short y)
        {
            Assert.AreEqual((x, y), (Session.Character.PositionX, Session.Character.PositionY));
        }

        private async Task WalkAsync(short x, short y, short speed, byte checksum)
        {
            LastWalk = await Service.WalkAsync(Session, x, y, speed, checksum);
        }

        private void TheLastWalkShouldBeRefused()
        {
            Assert.AreEqual(WalkResult.Refused, LastWalk);
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
            }, BarTarget, World.Clock.GetCurrentInstant());
        }

        private void TheBarShouldNotCompleteAfterFiveSeconds()
        {
            World.Clock.AdvanceSeconds(5);
            Assert.IsFalse(Session.Character.TryCompleteDelay(NosCore.Packets.Enumerations.DelayPacketType.ItemInUse,
                BarTarget, World.Clock.GetCurrentInstant()));
        }

        private short InitialX;
        private short InitialY;

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
            Session.Character.PositionX = 4;
            Session.Character.PositionY = 4;
            Session.Character.Speed = 20;
            InitialX = Session.Character.PositionX;
            InitialY = Session.Character.PositionY;
        }

        private void CharacterIsRooted()
        {
            World.Freeze<Mock<IBuffService>>()
                .Setup(b => b.HasEffect(It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(), BCardEffect.MoveMovementImpossible)).Returns(true);
            Service = World.UseReal<IWalkService, WalkService>().Create<GameObject.Services.MovementService.MovementService>();
        }

        private async Task WalkingToValidPosition()
        {
            var checksum = ((5 + 5) % 3) % 2;
            await WalkAsync(5, 5, 20, (byte)checksum);
        }

        private async Task WalkingWithHighSpeed()
        {
            await WalkAsync(5, 5, 100, 0);
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

        private void DirectionShouldBe_(byte direction) => Assert.AreEqual(direction, Session.Character.Direction);

        private void MapXYShouldBe_(short expectedX, short expectedY)
        {
            Assert.AreEqual(expectedX, Session.Character.MapX);
            Assert.AreEqual(expectedY, Session.Character.MapY);
        }

        [TestMethod]
        public async Task UsingPortalTooQuicklyIsTooSoon()
        {
            await new Spec("Using portal too quickly should show cooldown message")
                .Given(CharacterIsOnBaseMapAtTheEntryPortal)
                .And(CharacterRecentlyUsedPortal)
                .WhenAsync(UsingPortal)
                .Then(ThePortalShouldBeTooSoon)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingPortalWhenNoPortalNearbyShouldBeIgnored()
        {
            await new Spec("Using portal when no portal nearby should be ignored")
                .Given(CharacterIsOnBaseMapAtTheEntryPortal)
                .And(NoPortalNearby)
                .WhenAsync(UsingPortal)
                .Then(NoPortalShouldBeFound)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExitingFromNonBaseInstanceToBaseMapUsesCharacterMapXY()
        {
            await new Spec("Exit from non-base instance to base map uses character.MapId/MapX/MapY, not portal.DestinationX/Y")
                .Given(CharacterIsInsideMinilandWithBasePositionRememberedAt_, (short)1, (short)48, (short)132)
                .And(MinilandHasExitPortalToBaseMap)
                .And(PortalCooldownHasElapsed)
                .WhenAsync(UsingPortal)
                .Then(ChangeMapAsyncWasCalledWith_, (short)1, (short)48, (short)132)
                .And(ChangeMapInstanceAsyncWasNotCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringFromBaseMapToNonBaseInstanceUsesPortalDestination()
        {
            await new Spec("Entry from base map to non-base instance forwards the portal's destination coords")
                .Given(CharacterIsOnBaseMapAtTheEntryPortal)
                .And(BaseMapHasEntryPortalToMiniland)
                .And(PortalCooldownHasElapsed)
                .WhenAsync(UsingPortal)
                .Then(ChangeMapInstanceAsyncWasCalledWithPortalCoords_, (short)5, (short)8)
                .And(ChangeMapAsyncWasNotCalled)
                .ExecuteAsync();
        }

        private void CharacterIsOnBaseMapAtTheEntryPortal()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
            Session.Character.PositionX = 10;
            Session.Character.PositionY = 10;
        }

        private void CharacterRecentlyUsedPortal()
        {
            Session.Character.LastPortal = World.Clock.GetCurrentInstant();
        }

        private void NoPortalNearby()
        {
            DistanceCalculator.Setup(x => x.GetDistance(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns(100);
            World.Clock.Advance(NodaTime.Duration.FromSeconds(10));
            Session.Character.LastMove = World.Clock.GetCurrentInstant();
        }

        private void CharacterIsInsideMinilandWithBasePositionRememberedAt_(short baseMapId, short baseX, short baseY)
        {
            var miniland = World.GetMap(World.MinilandId);
            miniland.MapInstanceType = MapInstanceType.NormalInstance;
            Session.Character.MapInstance = miniland;
            Session.Character.MapId = baseMapId;
            Session.Character.MapX = baseX;
            Session.Character.MapY = baseY;
            Session.Character.PositionX = 3;
            Session.Character.PositionY = 8;
        }

        private void MinilandHasExitPortalToBaseMap()
        {
            var baseMap = World.GetBaseMap(1);
            Session.Character.MapInstance.Portals.Clear();
            Session.Character.MapInstance.Portals.Add(new NosCore.GameObject.Map.Portal
            {
                SourceX = 3,
                SourceY = 8,
                DestinationX = 999,
                DestinationY = 999,
                SourceMapInstanceId = Session.Character.MapInstance.MapInstanceId,
                DestinationMapInstanceId = baseMap.MapInstanceId,
                DestinationMapId = 1,
                Type = PortalType.MapPortal,
            });
            DistanceCalculator.Setup(x => x.GetDistance(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns(0);
        }

        private void BaseMapHasEntryPortalToMiniland()
        {
            var miniland = World.GetMap(World.MinilandId);
            miniland.MapInstanceType = MapInstanceType.NormalInstance;
            Session.Character.MapInstance.Portals.Clear();
            Session.Character.MapInstance.Portals.Add(new NosCore.GameObject.Map.Portal
            {
                SourceX = 10,
                SourceY = 10,
                DestinationX = 5,
                DestinationY = 8,
                SourceMapInstanceId = Session.Character.MapInstance.MapInstanceId,
                DestinationMapInstanceId = miniland.MapInstanceId,
                DestinationMapId = 20001,
                Type = PortalType.Miniland,
            });
            DistanceCalculator.Setup(x => x.GetDistance(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns(0);
        }

        private void PortalCooldownHasElapsed()
        {
            World.Clock.Advance(NodaTime.Duration.FromSeconds(10));
            Session.Character.LastMove = World.Clock.GetCurrentInstant();
            Session.Character.LastPortal = World.Clock.GetCurrentInstant() - NodaTime.Duration.FromSeconds(20);
        }

        private void ChangeMapAsyncWasCalledWith_(short expectedMapId, short expectedMapX, short expectedMapY)
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(
                Session,
                It.Is<short?>(v => v == expectedMapId),
                It.Is<short?>(v => v == expectedMapX),
                It.Is<short?>(v => v == expectedMapY)), Times.Once);
        }

        private void ChangeMapAsyncWasNotCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(
                It.IsAny<ClientSession>(), It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);
        }

        private void ChangeMapInstanceAsyncWasCalledWithPortalCoords_(short expectedX, short expectedY)
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(
                Session,
                It.IsAny<Guid>(),
                It.Is<int?>(v => v == expectedX),
                It.Is<int?>(v => v == expectedY)), Times.Once);
        }

        private void ChangeMapInstanceAsyncWasNotCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(
                It.IsAny<ClientSession>(), It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
        }

        private async Task UsingPortal()
        {
            LastPortal = await Service.UsePortalAsync(Session);
        }

        private void ThePortalShouldBeTooSoon()
        {
            Assert.AreEqual(PortalResult.TooSoon, LastPortal);
        }

        private void NoPortalShouldBeFound()
        {
            Assert.AreEqual(PortalResult.NoPortal, LastPortal);
        }


        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

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
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.PathFinder.Interfaces;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class PreqPacketHandlerTests
    {
        private PreqPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IHeuristic> DistanceCalculator = null!;
        private Mock<IMinilandService> MinilandService = null!;
        private Mock<IMapChangeService> MapChangeService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            DistanceCalculator = new Mock<IHeuristic>();
            MinilandService = new Mock<IMinilandService>();
            MapChangeService = new Mock<IMapChangeService>();

            DistanceCalculator.Setup(x => x.GetDistance(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns(10);

            MinilandService.Setup(x => x.GetMinilandPortals(It.IsAny<long>()))
                .Returns(new List<NosCore.GameObject.Map.Portal>());

            Handler = new PreqPacketHandler(
                TestHelpers.Instance.MapInstanceAccessorService,
                MinilandService.Object,
                DistanceCalculator.Object,
                TestHelpers.Instance.Clock,
                MapChangeService.Object);
        }

        [TestMethod]
        public async Task UsingPortalTooQuicklyShouldShowCooldownMessage()
        {
            await new Spec("Using portal too quickly should show cooldown message")
                .Given(CharacterIsOnMap)
                .And(CharacterRecentlyUsedPortal)
                .WhenAsync(UsingPortal)
                .Then(ShouldReceiveCooldownMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingPortalWhenNoPortalNearbyShouldBeIgnored()
        {
            await new Spec("Using portal when no portal nearby should be ignored")
                .Given(CharacterIsOnMap)
                .And(NoPortalNearby)
                .WhenAsync(UsingPortal)
                .Then(NoPacketShouldBeSent)
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
                .Given(CharacterIsOnMap)
                .And(BaseMapHasEntryPortalToMiniland)
                .And(PortalCooldownHasElapsed)
                .WhenAsync(UsingPortal)
                .Then(ChangeMapInstanceAsyncWasCalledWithPortalCoords_, (short)5, (short)8)
                .And(ChangeMapAsyncWasNotCalled)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.Character.PositionX = 10;
            Session.Character.PositionY = 10;
        }

        private void CharacterRecentlyUsedPortal()
        {
            Session.Character.LastPortal = TestHelpers.Instance.Clock.GetCurrentInstant();
        }

        private void NoPortalNearby()
        {
            DistanceCalculator.Setup(x => x.GetDistance(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns(100);
            TestHelpers.Instance.Clock.Advance(NodaTime.Duration.FromSeconds(10));
            Session.Character.LastMove = TestHelpers.Instance.Clock.GetCurrentInstant();
        }

        private void CharacterIsInsideMinilandWithBasePositionRememberedAt_(short baseMapId, short baseX, short baseY)
        {
            var miniland = TestHelpers.Instance.MapInstanceAccessorService.GetMapInstance(TestHelpers.Instance.MinilandId)!;
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
            var baseMap = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
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
            var miniland = TestHelpers.Instance.MapInstanceAccessorService.GetMapInstance(TestHelpers.Instance.MinilandId)!;
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
            TestHelpers.Instance.Clock.Advance(NodaTime.Duration.FromSeconds(10));
            Session.Character.LastMove = TestHelpers.Instance.Clock.GetCurrentInstant();
            Session.Character.LastPortal = TestHelpers.Instance.Clock.GetCurrentInstant() - NodaTime.Duration.FromSeconds(20);
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
            await Handler.ExecuteAsync(new PreqPacket(), Session);
        }

        private void ShouldReceiveCooldownMessage()
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.WillMoveShortly, packet.Message);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

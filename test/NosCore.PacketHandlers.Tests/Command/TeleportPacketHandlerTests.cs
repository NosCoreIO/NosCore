//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.PacketHandlers.Command;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class TeleportPacketHandlerTests
    {
        private TeleportPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;
        private Mock<IMapChangeService> MapChangeService = null!;
        private readonly ILogger<TeleportPacketHandler> Logger = new Mock<ILogger<TeleportPacketHandler>>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            MapChangeService = new Mock<IMapChangeService>();

            Handler = new TeleportPacketHandler(
                Logger,
                MapChangeService.Object,
                TestHelpers.Instance.GameLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task TeleportingToPlayerShouldChangeMapInstance()
        {
            await new Spec("Teleporting to player should change map instance")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .WhenAsync(TeleportingToPlayer)
                .Then(MapChangeServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeleportingToMapByIdShouldChangeMap()
        {
            await new Spec("Teleporting to map by id should change map")
                .Given(CharacterIsOnMap)
                .WhenAsync(TeleportingToMapById)
                .Then(MapChangeByIdShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeleportingToUnknownPlayerShouldLogError()
        {
            await new Spec("Teleporting to unknown player should log error")
                .Given(CharacterIsOnMap)
                .WhenAsync(TeleportingToUnknownPlayer)
                .Then(NoMapChangeShouldOccur)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeleportingToPlayerForwardsTargetMapXYNotSelfPosition()
        {
            await new Spec("Teleport-to-player forwards the target's MapX/MapY (not the caller's)")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnlineAtMapXY_, (short)77, (short)88)
                .And(CallerIsAtDifferentPosition_, (short)10, (short)10)
                .WhenAsync(TeleportingToPlayer)
                .Then(MapChangeInstanceCalledWithCoords_, 77, 88)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeleportingToMapByIdForwardsPacketMapXYCoordinates()
        {
            await new Spec("Teleport-by-map-id forwards the packet's MapX/MapY to ChangeMapAsync")
                .Given(CharacterIsOnMap)
                .WhenAsync(TeleportingToMapByIdAt_, (short)50, (short)50)
                .Then(MapChangeByIdCalledWithMapAndCoords_, (short)1, (short)50, (short)50)
                .ExecuteAsync();
        }

        private void TargetIsOnlineAtMapXY_(short x, short y)
        {
            TargetSession.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            TargetSession.Character.MapX = x;
            TargetSession.Character.MapY = y;
        }

        private void CallerIsAtDifferentPosition_(short x, short y)
        {
            Session.Character.MapX = x;
            Session.Character.MapY = y;
        }

        private async Task TeleportingToMapByIdAt_(short x, short y)
        {
            await Handler.ExecuteAsync(new TeleportPacket
            {
                TeleportArgument = "1",
                MapX = x,
                MapY = y,
            }, Session);
        }

        private void MapChangeInstanceCalledWithCoords_(int expectedX, int expectedY)
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(
                Session,
                It.IsAny<System.Guid>(),
                It.Is<int?>(v => v == expectedX),
                It.Is<int?>(v => v == expectedY)), Times.Once);
        }

        private void MapChangeByIdCalledWithMapAndCoords_(short expectedMapId, short expectedX, short expectedY)
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(
                Session,
                It.Is<short?>(v => v == expectedMapId),
                It.Is<short?>(v => v == expectedX),
                It.Is<short?>(v => v == expectedY)), Times.Once);
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void TargetIsOnline()
        {
            TargetSession.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task TeleportingToPlayer()
        {
            await Handler.ExecuteAsync(new TeleportPacket
            {
                TeleportArgument = TargetSession.Character.Name
            }, Session);
        }

        private async Task TeleportingToMapById()
        {
            await Handler.ExecuteAsync(new TeleportPacket
            {
                TeleportArgument = "1",
                MapX = 50,
                MapY = 50
            }, Session);
        }

        private async Task TeleportingToUnknownPlayer()
        {
            await Handler.ExecuteAsync(new TeleportPacket
            {
                TeleportArgument = "UnknownPlayerName123"
            }, Session);
        }

        private void MapChangeServiceShouldBeCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(
                It.IsAny<ClientSession>(),
                It.IsAny<System.Guid>(),
                It.IsAny<int?>(),
                It.IsAny<int?>()), Times.Once);
        }

        private void MapChangeByIdShouldBeCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(
                It.IsAny<ClientSession>(),
                It.IsAny<short?>(),
                It.IsAny<short?>(),
                It.IsAny<short?>()), Times.Once);
        }

        private void NoMapChangeShouldOccur()
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(
                It.IsAny<ClientSession>(),
                It.IsAny<System.Guid>(),
                It.IsAny<int?>(),
                It.IsAny<int?>()), Times.Never);
            MapChangeService.Verify(x => x.ChangeMapAsync(
                It.IsAny<ClientSession>(),
                It.IsAny<short?>(),
                It.IsAny<short?>(),
                It.IsAny<short?>()), Times.Never);
        }
    }
}

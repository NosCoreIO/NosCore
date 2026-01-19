//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
                .Returns(new List<NosCore.GameObject.ComponentEntities.Entities.Portal>());

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

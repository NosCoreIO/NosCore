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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class PreqPacketHandlerTests
    {
        private TestWorld World;
        private PreqPacketHandler Handler;
        private ClientSession Session;
        private Mock<IMovementService> MovementService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            MovementService = World.Freeze<Mock<IMovementService>>();
            Handler = World.Create<PreqPacketHandler>();
        }

        [TestMethod]
        public async Task APortalUsedTooSoonShowsTheCooldownMessage()
        {
            await new Spec("A portal used too soon shows the cooldown message")
                .Given(UsingThePortalWillBe_, PortalResult.TooSoon)
                .WhenAsync(UsingPortal)
                .Then(ShouldReceiveCooldownMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AUsedPortalSendsNothingMore()
        {
            await new Spec("A used portal leaves the map change to the movement service")
                .Given(UsingThePortalWillBe_, PortalResult.Used)
                .WhenAsync(UsingPortal)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoPortalNearbySendsNothing()
        {
            await new Spec("No portal nearby sends nothing")
                .Given(UsingThePortalWillBe_, PortalResult.NoPortal)
                .WhenAsync(UsingPortal)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void UsingThePortalWillBe_(PortalResult result)
        {
            MovementService.Setup(x => x.UsePortalAsync(It.IsAny<ClientSession>())).ReturnsAsync(result);
        }

        private Task UsingPortal()
        {
            return Handler.ExecuteAsync(new PreqPacket(), Session);
        }

        private void ShouldReceiveCooldownMessage()
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().Single();
            Assert.AreEqual(Game18NConstString.WillMoveShortly, packet.Message);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

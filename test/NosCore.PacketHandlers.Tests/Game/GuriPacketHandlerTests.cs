//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GuriRunnerService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GuriPacketHandlerTests
    {
        private GuriPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IGuriRunnerService> GuriRunnerService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            GuriRunnerService = new Mock<IGuriRunnerService>();
            Handler = new GuriPacketHandler(GuriRunnerService.Object);
        }

        [TestMethod]
        public void HandlerCanBeConstructed()
        {
            new Spec("Handler can be constructed")
                .Then(HandlerShouldNotBeNull)
                .Execute();
        }

        [TestMethod]
        public async Task GuriPacketShouldCallGuriRunnerService()
        {
            await new Spec("Guri packet should call guri runner service")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingGuriPacket)
                .Then(GuriRunnerServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GuriPacketWithTypeShouldPassCorrectData()
        {
            await new Spec("Guri packet with type should pass correct data")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingGuriPacketWithType)
                .Then(GuriRunnerServiceShouldBeCalledWithCorrectType)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
        }

        private async Task ExecutingGuriPacket()
        {
            await Handler.ExecuteAsync(new GuriPacket(), Session);
        }

        private async Task ExecutingGuriPacketWithType()
        {
            await Handler.ExecuteAsync(new GuriPacket { Type = GuriPacketType.Title }, Session);
        }

        private void HandlerShouldNotBeNull()
        {
            Assert.IsNotNull(Handler);
        }

        private void GuriRunnerServiceShouldBeCalled()
        {
            GuriRunnerService.Verify(x => x.GuriLaunch(
                It.IsAny<ClientSession>(),
                It.IsAny<GuriPacket>()), Times.Once);
        }

        private void GuriRunnerServiceShouldBeCalledWithCorrectType()
        {
            GuriRunnerService.Verify(x => x.GuriLaunch(
                It.IsAny<ClientSession>(),
                It.Is<GuriPacket>(p => p.Type == GuriPacketType.Title)), Times.Once);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class ClientDirPacketHandlerTests
    {
        private ClientDirPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new ClientDirPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task ChangingDirectionForPlayerShouldExecute()
        {
            await new Spec("Changing direction for player should execute")
                .Given(CharacterIsOnMap)
                .WhenAsync(ChangingDirectionForPlayer)
                .Then(HandlerShouldComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingDirectionForUnknownVisualTypeShouldBeIgnored()
        {
            await new Spec("Changing direction for unknown visual type should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(ChangingDirectionForUnknownVisualType)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task ChangingDirectionForPlayer()
        {
            await Handler.ExecuteAsync(new ClientDirPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.VisualId,
                Direction = 2
            }, Session);
        }

        private async Task ChangingDirectionForUnknownVisualType()
        {
            await Handler.ExecuteAsync(new ClientDirPacket
            {
                VisualType = (VisualType)99,
                VisualId = Session.Character.VisualId,
                Direction = 2
            }, Session);
        }

        private void HandlerShouldComplete()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

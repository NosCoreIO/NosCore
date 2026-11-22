//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.TalentArenaService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class TawPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<ITalentArenaService> Service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Service = World.Freeze<Mock<ITalentArenaService>>();
        }

        [TestMethod]
        public async Task WatchingAFighterAsksTheArenaByName()
        {
            await new Spec("Watching a fighter asks the arena by name")
                .WhenAsync(SendingThePacket)
                .Then(TheServiceIsCalled)
                .ExecuteAsync();
        }

        private Task SendingThePacket() =>
            World.Create<TawPacketHandler>().ExecuteAsync(new TawPacket("fighter"), Session);

        private void TheServiceIsCalled() => Service.Verify(s => s.WatchAsync(Session, "fighter"), Times.Once);
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GitPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IMapButtonService> Service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Service = World.Freeze<Mock<IMapButtonService>>();
        }

        [TestMethod]
        public async Task PressingAButtonUsesItForTheCharacter()
        {
            await new Spec("Pressing a button uses it for the character")
                .WhenAsync(SendingThePacket)
                .Then(TheServiceIsCalled)
                .ExecuteAsync();
        }

        private Task SendingThePacket() =>
            World.Create<GitPacketHandler>().ExecuteAsync(new GitPacket { ButtonId = 3 }, Session);

        private void TheServiceIsCalled() => Service.Verify(s => s.UseAsync(It.IsAny<PlayerComponentBundle>(), 3), Times.Once);
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class MinilandDisconnectHandlerTests
    {
        private TestWorld World;
        private ClientSession _owner;
        private ClientSession _visitor;
        private Mock<IMapInstanceGeneratorService> _maps;
        private Guid _miniland;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _maps = World.Freeze<Mock<IMapInstanceGeneratorService>>();
            var minilands = World.Freeze<Mock<IMinilandService>>();
            _owner = await World.CreateSessionAsync();
            _visitor = await World.CreateSessionAsync();
            _miniland = _visitor.Character.MapInstanceId;
            var ownerId = _owner.Character.CharacterId;
            minilands.Setup(s => s.DeleteMinilandAsync(ownerId)).ReturnsAsync(_miniland);
        }

        [TestMethod]
        public async Task AVisitorOnTheWayOutIsLetFinishBeforeTheMinilandGoes()
        {
            _visitor.Character.IsDisconnecting = true;

            var removal = World.Create<MinilandDisconnectHandler>().HandleDisconnectAsync(_owner);
            await Task.Delay(100);

            Assert.IsFalse(removal.IsCompleted, "the removal waits for the visitor");
            _maps.Verify(s => s.RemoveMapAsync(It.IsAny<Guid>()), Times.Never);

            await _visitor.OnDisconnectedAsync();
            await removal;

            _maps.Verify(s => s.RemoveMapAsync(_miniland), Times.Once);
        }

        [TestMethod]
        public async Task AVisitorStillPlayingDoesNotHoldTheMinilandUp()
        {
            await World.Create<MinilandDisconnectHandler>().HandleDisconnectAsync(_owner);

            _maps.Verify(s => s.RemoveMapAsync(_miniland), Times.Once);
        }

        [TestMethod]
        public void TheMinilandGoesAfterEveryOtherStep()
        {
            var miniland = World.Create<MinilandDisconnectHandler>();

            Assert.IsTrue(miniland.Order > World.Create<TimeSpaceDisconnectHandler>().Order);
            Assert.IsTrue(miniland.Order > World.Create<SaveDisconnectHandler>().Order);
        }
    }
}

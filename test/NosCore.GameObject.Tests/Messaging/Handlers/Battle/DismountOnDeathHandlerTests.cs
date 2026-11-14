//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class DismountOnDeathHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<ITransformationService> _transformations;
        private DismountOnDeathHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _transformations = World.Freeze<Mock<ITransformationService>>();
            _handler = World.Create<DismountOnDeathHandler>();
        }

        [TestMethod]
        public async Task ARiderWhoDiesIsTakenOffTheMount()
        {
            _session.Character.IsVehicled = true;

            await _handler.Handle(new EntityDiedEvent(_session.Character, null));

            _transformations.Verify(t => t.RemoveVehicleAsync(_session), Times.Once);
        }

        [TestMethod]
        public async Task APlayerOnFootIsLeftAlone()
        {
            await _handler.Handle(new EntityDiedEvent(_session.Character, null));

            _transformations.Verify(t => t.RemoveVehicleAsync(It.IsAny<ClientSession>()), Times.Never);
        }
    }
}

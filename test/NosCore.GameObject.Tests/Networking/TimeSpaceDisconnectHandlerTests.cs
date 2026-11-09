//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class TimeSpaceDisconnectHandlerTests
    {
        private ClientSession _session = null!;
        private readonly Mock<IScriptedInstanceService> _scriptedInstances = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        private sealed class EmptyScript : InstanceScript;

        [TestMethod]
        public async Task LoggingOutInsideARunOffersItsRoomsForRemoval()
        {
            var room = _session.Character.MapInstance;
            var roomId = room.MapInstanceId;
            var run = new ScriptedInstanceRun(new ScriptedInstance(), new EmptyScript(),
                new Dictionary<int, MapInstance> { [1] = room },
                new Dictionary<short, NpcMonsterDto>(), TestHelpers.Instance.Clock,
                TestHelpers.Instance.SessionRegistry,
                new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), NullLogger.Instance,
                TimeSpan.FromSeconds(30));
            _scriptedInstances.Setup(s => s.GetRun(roomId)).Returns(run);

            await new TimeSpaceDisconnectHandler(_scriptedInstances.Object).HandleDisconnectAsync(_session);

            _scriptedInstances.Verify(s => s.DisposeIfEmptyAsync(run), Times.Once);
        }

        [TestMethod]
        public async Task LoggingOutOutsideARunRemovesNothing()
        {
            await new TimeSpaceDisconnectHandler(_scriptedInstances.Object).HandleDisconnectAsync(_session);

            _scriptedInstances.Verify(s => s.DisposeIfEmptyAsync(It.IsAny<ScriptedInstanceRun>()), Times.Never);
        }

        [TestMethod]
        public void TheRunIsCheckedOnlyOnceThePlayerHasLeftTheMap()
        {
            ISessionDisconnectHandler map = new MapDisconnectHandler();
            ISessionDisconnectHandler timeSpace = new TimeSpaceDisconnectHandler(_scriptedInstances.Object);

            Assert.IsTrue(timeSpace.Order > map.Order);
        }
    }
}

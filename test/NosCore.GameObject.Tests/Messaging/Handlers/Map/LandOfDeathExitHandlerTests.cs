//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.LandOfDeathService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Map
{
    [TestClass]
    public class LandOfDeathExitHandlerTests
    {
        private TestWorld World;
        private Mock<ILandOfDeathService> _landOfDeath;
        private LandOfDeathExitHandler _handler;
        private ClientSession _session;
        private MapInstance _land;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _landOfDeath = World.Freeze<Mock<ILandOfDeathService>>();
            _handler = World.Create<LandOfDeathExitHandler>();
            _session = await World.CreateSessionAsync();
            _land = World.MapInstanceGeneratorService.CreateMapInstance(World.GetBaseMap(0).Map, Guid.NewGuid(), false,
                MapInstanceType.LodInstance);
        }

        [TestMethod]
        public async Task LeavingTheLandTakesTheClockAway()
        {
            await new Spec("A player who walks out of the Land of Death loses its clock")
                .WhenAsync(MovingFrom_To_, true, false)
                .Then(TheClockShouldBeCleared_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringTheLandKeepsTheClock()
        {
            await new Spec("A player who walks into the Land of Death keeps its clock")
                .WhenAsync(MovingFrom_To_, false, true)
                .Then(TheClockShouldBeCleared_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherMapsLeaveTheClockAlone()
        {
            await new Spec("Moving between ordinary maps does not touch the clock")
                .WhenAsync(MovingFrom_To_, false, false)
                .Then(TheClockShouldBeCleared_, false)
                .ExecuteAsync();
        }

        private Task MovingFrom_To_(bool fromLand, bool toLand) =>
            _handler.Handle(new MapInstanceEnteredEvent(_session, toLand ? _land : World.GetBaseMap(0),
                fromLand ? _land : World.GetBaseMap(0)));

        private void TheClockShouldBeCleared_(bool cleared) =>
            _landOfDeath.Verify(s => s.LeaveAsync(_session), cleared ? Times.Once() : Times.Never());
    }
}

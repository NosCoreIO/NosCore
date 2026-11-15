//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Tests.Shared;
using SpecLight;
using Act5Ship = NosCore.GameObject.Services.Act5ShipService.Act5ShipService;

namespace NosCore.GameObject.Tests.Services.Act5ShipService
{
    [TestClass]
    public class Act5ShipServiceTests
    {
        private TestWorld World;
        private Act5Ship _service;
        private ClientSession _session;
        private Mock<IScriptedInstanceService> _scriptedInstances;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _scriptedInstances = World.Freeze<Mock<IScriptedInstanceService>>();
            _service = World.Create<Act5Ship>();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(0);
        }

        [TestMethod]
        public async Task AnyoneOnTheHarbourMayBoardForFree()
        {
            await new Spec("A player on the harbour is taken to the next ship without paying")
                .Given(ThePlayerHas_Gold, 0L)
                .WhenAsync(Boarding)
                .Then(AShipShouldBeLaunched)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadPlayerCannotBoard()
        {
            await new Spec("A dead player stays where they are")
                .Given(ThePlayerIsDead)
                .WhenAsync(Boarding)
                .Then(NoShipShouldBeLaunched)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingTheShipDoesNothingAshore()
        {
            await new Spec("Someone who is not aboard has no ship to leave")
                .WhenAsync(Leaving)
                .Then(ThePlayerShouldStillBeOnTheHarbour)
                .ExecuteAsync();
        }

        private void ThePlayerHas_Gold(long gold) => _session.Character.Gold = gold;

        private void ThePlayerIsDead() => _session.Character.IsAlive = false;

        private Task Boarding() => _service.BoardAsync(_session);

        private Task Leaving() => _service.DisembarkAsync(_session);

        private void ThePlayerShouldStillBeOnTheHarbour() =>
            Assert.AreEqual(World.GetBaseMap(0).MapInstanceId, _session.Character.MapInstance.MapInstanceId);

        private void NoShipShouldBeLaunched() =>
            _scriptedInstances.Verify(s => s.LoadScriptAsync(It.IsAny<string>()), Times.Never);

        private void AShipShouldBeLaunched() =>
            _scriptedInstances.Verify(s => s.LoadScriptAsync(Act5Ship.Script), Times.Once);
    }
}

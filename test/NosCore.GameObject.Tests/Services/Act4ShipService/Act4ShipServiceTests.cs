//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using Act4Ship = NosCore.GameObject.Services.Act4ShipService.Act4ShipService;

namespace NosCore.GameObject.Tests.Services.Act4ShipService
{
    [TestClass]
    public class Act4ShipServiceTests
    {
        private TestWorld World;
        private Act4Ship _service;
        private ClientSession _session;
        private Mock<IScriptedInstanceService> _scriptedInstances;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _scriptedInstances = World.Freeze<Mock<IScriptedInstanceService>>();
            _service = World.Create<Act4Ship>();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(0);
            _session.Character.Faction = FactionType.Angel;
            _session.Character.Gold = Act4Ship.Fare;
        }

        [TestMethod]
        public async Task OnlyPlayersWithAPowerOfProtectionMayBoard()
        {
            await new Spec("A neutral player is told only those with a Power of Protection can go")
                .Given(BoardingIsOpen)
                .And(ThePlayerIsNeutral)
                .WhenAsync(Boarding)
                .Then(TheyShouldBeTurnedAwayWith_, Game18NConstString.FactionRequired)
                .And(NoShipShouldBeLaunched)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodyBoardsBeforeTheShipIsBack()
        {
            await new Spec("Outside the boarding window the ship has not returned yet")
                .WhenAsync(Boarding)
                .Then(TheyShouldBeTurnedAwayWith_, Game18NConstString.ShipHasNotReturnedYet)
                .And(NoShipShouldBeLaunched)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayerWhoCannotPayTheFareStaysAshore()
        {
            await new Spec("A player short of the fare is told so and stays ashore")
                .Given(BoardingIsOpen)
                .And(ThePlayerIsShortOfTheFare)
                .WhenAsync(Boarding)
                .Then(TheyShouldBeToldTheyLackGold)
                .And(NoShipShouldBeLaunched)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayerWhoCanPayIsTakenToTheShip()
        {
            await new Spec("A player with a Power of Protection and the fare is taken to the ship")
                .Given(BoardingIsOpen)
                .WhenAsync(Boarding)
                .Then(AShipShouldBeLaunched)
                .ExecuteAsync();
        }

        [TestMethod]
        public void BoardingCannotOpenTwiceAtOnce()
        {
            Assert.IsTrue(_service.TryStart());
            Assert.IsFalse(_service.TryStart());
        }

        private void BoardingIsOpen() => Assert.IsTrue(_service.TryStart());

        private void ThePlayerIsNeutral() => _session.Character.Faction = FactionType.Neutral;

        private void ThePlayerIsShortOfTheFare() => _session.Character.Gold = Act4Ship.Fare - 1;

        private Task Boarding() => _service.BoardAsync(_session);

        private void TheyShouldBeTurnedAwayWith_(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == message));

        private void TheyShouldBeToldTheyLackGold() =>
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.NotEnoughGold));

        private void NoShipShouldBeLaunched() =>
            _scriptedInstances.Verify(s => s.LoadScriptAsync(It.IsAny<string>()), Times.Never);

        private void AShipShouldBeLaunched() =>
            _scriptedInstances.Verify(s => s.LoadScriptAsync(Act4Ship.Script), Times.Once);
    }
}

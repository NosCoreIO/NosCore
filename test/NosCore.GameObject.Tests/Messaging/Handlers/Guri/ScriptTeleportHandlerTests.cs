//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class ScriptTeleportHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IMapChangeService> _mapChange;
        private ScriptTeleportHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _mapChange = World.Freeze<Mock<IMapChangeService>>();
            _handler = World.Create<ScriptTeleportHandler>();
        }

        [TestMethod]
        public async Task AcceptingTheOfferedTeleportCarriesThePlayerThere()
        {
            await new Spec("Accepting the teleport a run step offered carries the player to its map and cell")
                .Given(ARunStepOfferedTheTeleportTo___, (short)1, (short)81, (short)33)
                .WhenAsync(TheClientAccepts___, 1, 81L, 33L)
                .Then(ThePlayerIsCarriedTo___, (short)1, (short)81, (short)33)
                .And(NoTeleportIsOfferedAnyMore)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAnswerNamingAnotherDestinationMovesNobody()
        {
            await new Spec("An accept naming a map the question did not offer moves nobody")
                .Given(ARunStepOfferedTheTeleportTo___, (short)1, (short)81, (short)33)
                .WhenAsync(TheClientAccepts___, 145, 81L, 33L)
                .Then(NobodyIsCarried)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAcceptWithoutAQuestionMovesNobody()
        {
            await new Spec("An accept sent while no question is open moves nobody")
                .WhenAsync(TheClientAccepts___, 1, 81L, 33L)
                .Then(NobodyIsCarried)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DecliningClosesTheQuestion()
        {
            await new Spec("Declining leaves the player where they stand and closes the question")
                .Given(ARunStepOfferedTheTeleportTo___, (short)1, (short)81, (short)33)
                .WhenAsync(TheClientDeclines)
                .AndAsync(TheClientAccepts___, 1, 81L, 33L)
                .Then(NobodyIsCarried)
                .And(NoTeleportIsOfferedAnyMore)
                .ExecuteAsync();
        }

        private void ARunStepOfferedTheTeleportTo___(short mapId, short x, short y) =>
            _session.Character.OfferedScriptTeleport = new ScriptDto
            {
                Id = Guid.NewGuid(), ScriptId = 1, ScriptStepId = 474, StepType = "run", Argument1 = 200,
                TeleportMapId = mapId, TeleportX = x, TeleportY = y
            };

        private Task TheClientAccepts___(int mapId, long x, long y) =>
            _handler.Handle(new GuriPacketReceivedEvent(_session, new GuriPacket
            {
                Type = GuriPacketType.ScriptTeleport, Argument = mapId, VisualId = x, Data = y
            }));

        private Task TheClientDeclines() =>
            _handler.Handle(new GuriPacketReceivedEvent(_session, new GuriPacket { Type = GuriPacketType.ScriptTeleportDeclined }));

        private void ThePlayerIsCarriedTo___(short mapId, short x, short y) =>
            _mapChange.Verify(m => m.ChangeMapAsync(_session, mapId, x, y), Times.Once);

        private void NobodyIsCarried() =>
            _mapChange.Verify(m => m.ChangeMapAsync(It.IsAny<ClientSession>(), It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);

        private void NoTeleportIsOfferedAnyMore() =>
            Assert.IsNull(_session.Character.OfferedScriptTeleport);
    }
}

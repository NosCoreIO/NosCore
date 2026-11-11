//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Tests.Services.ArenaService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class GoToArenaHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IMapChangeService> _mapChangeService;
        private MapInstance _arena;
        private GoToArenaHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(1);
            _arena = World.GetBaseMap(0);
            _arena.Map = ArenaTestMap.Create();
            _arena.MapInstanceType = MapInstanceType.ArenaInstance;
            _mapChangeService = World.Freeze<Mock<IMapChangeService>>();
            _handler = World.Create<GoToArenaHandler>();
            _session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto> { new() { MapNpcId = 100, MapId = _session.Character.MapInstance.Map.MapId, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1 } });
            _session.Character.LastPortal = World.Clock.GetCurrentInstant() - NodaTime.Duration.FromSeconds(10);
        }

        [TestMethod]
        public async Task RunnerPropertyMatchesGoToArena()
        {
            await new Spec("GoToArenaHandler declares NrunRunnerType.GoToArena")
                .Then(RunnerShouldBeGoToArena)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringTheArenaCostsFiveHundredGold()
        {
            await new Spec("The individual arena takes 500 gold and drops the player on its floor")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(TheNpcIsAskedForArenaType_, (short)0)
                .Then(CharacterGoldShouldBe_, 9_500L)
                .And(ThePlayerShouldBeSentToTheArena)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoEntryFromInsideAnInstance()
        {
            await new Spec("A player already inside an instance cannot pay into the arena")
                .Given(CharacterGoldIs_, 10_000L)
                .And(ThePlayerIsInsideAnInstance)
                .WhenAsync(TheNpcIsAskedForArenaType_, (short)0)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NotEnoughGoldKeepsThePlayerOut()
        {
            await new Spec("Without 500 gold the player is told so and stays where they are")
                .Given(CharacterGoldIs_, 499L)
                .WhenAsync(TheNpcIsAskedForArenaType_, (short)0)
                .Then(CharacterGoldShouldBe_, 499L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .And(TheyShouldBeTold_, Game18NConstString.NotEnoughGold)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFreshPortalBlocksTheEntry()
        {
            await new Spec("Within four seconds of changing map the player cannot enter yet")
                .Given(CharacterGoldIs_, 10_000L)
                .And(ThePlayerJustChangedMap)
                .WhenAsync(TheNpcIsAskedForArenaType_, (short)0)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .And(TheyShouldBeTold_, Game18NConstString.WillMoveShortly)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnArenaThatIsNotLoadedIsIgnored()
        {
            await new Spec("The Arena of Glory is not loaded in this world, so nothing is charged")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(TheNpcIsAskedForArenaType_, (short)1)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANonNpcTargetIsIgnored()
        {
            await new Spec("Only an NPC can send the player to the arena")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(ArenaTypeZeroIsAskedOfANonNpc)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        private void RunnerShouldBeGoToArena() => Assert.AreEqual(NrunRunnerType.GoToArena, _handler.Runner);

        private void CharacterGoldIs_(long gold) => _session.Character.Gold = gold;

        private void ThePlayerIsInsideAnInstance() =>
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.TimeSpaceInstance;

        private void ThePlayerJustChangedMap() =>
            _session.Character.LastPortal = World.Clock.GetCurrentInstant();

        private Task TheNpcIsAskedForArenaType_(short type) =>
            _handler.HandleAsync(_session, _session.Character.MapInstance.GetNpcById(100), Packet(type));

        private Task ArenaTypeZeroIsAskedOfANonNpc() =>
            _handler.HandleAsync(_session, new Mock<IAliveEntity>().Object, Packet(0));

        private static NrunPacket Packet(short type) => new()
        {
            Runner = NrunRunnerType.GoToArena,
            Type = type,
            VisualType = VisualType.Npc,
            VisualId = 100,
        };

        private void CharacterGoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void ThePlayerShouldBeSentToTheArena() =>
            _mapChangeService.Verify(x => x.ChangeMapInstanceAsync(_session, _arena.MapInstanceId,
                It.Is<int?>(v => v >= 0 && v <= 3), It.Is<int?>(v => v >= 0 && v <= 1)), Times.Once);

        private void NoMapChangeShouldHaveBeenInvoked() =>
            _mapChangeService.Verify(x => x.ChangeMapInstanceAsync(It.IsAny<ClientSession>(), It.IsAny<System.Guid>(),
                It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);

        private void TheyShouldBeTold_(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == message));
    }
}

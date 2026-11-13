//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.PathfindingService;
using NosCore.PathFinder.Interfaces;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // Ticks a real monster on the shared test map (walkable: all of rows 0, 4 and 5, and x >= 4 on
    // every row) with a real aggro service and pathfinder, a fixed clock and a battle service
    // that only records the swings.
    [TestClass]
    public class MonsterAiTests
    {
        private TestWorld World;
        private ClientSession _session;
        private MapInstance _map;
        private Mock<IBattleService> _battle;
        private AggroService _aggro;
        private MonsterAi _ai;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            _session.Character.MapInstance = _map;
            _map.Map = TestMap();

            _battle = new Mock<IBattleService>();
            AcceptSwings(true);
            _aggro = World.Create<AggroService>();
            _ai = new MonsterAi(
                _battle.Object,
                _aggro,
                World.Create<GameObject.Services.PathfindingService.PathfindingService>(),
                World.SessionRegistry,
                World.DistanceCalculator,
                new Mock<INpcCombatCatalog>().Object,
                new Mock<IRandomProvider>().Object,
                World.Clock,
                new Dictionary<short, SkillDto>(),
                NullLogger<MonsterAi>.Instance);
        }

        [TestMethod]
        public void ConstructsWithAllDependencies()
        {
            var ai = new MonsterAi(
                new Mock<IBattleService>().Object,
                new Mock<IAggroService>().Object,
                new Mock<IPathfindingService>().Object,
                new Mock<ISessionRegistry>().Object,
                new Mock<IHeuristic>().Object,
                new Mock<INpcCombatCatalog>().Object,
                new Mock<IRandomProvider>().Object,
                new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0)),
                new Dictionary<short, SkillDto>(),
                new Mock<ILogger<MonsterAi>>().Object);

            Assert.IsNotNull(ai);
            Assert.IsInstanceOfType(ai, typeof(IMonsterAi));
        }

        [TestMethod]
        public async Task HostileMonsterAttacksAPlayerWithinReach()
        {
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 2);

            var acted = await _ai.TickAsync(monster);

            Assert.IsTrue(acted);
            SwingsAtThePlayer(Times.Once());
        }

        [TestMethod]
        public async Task HostileMonsterChasesAPlayerOutOfReachWithoutSwinging()
        {
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 5);

            var acted = await _ai.TickAsync(monster);

            Assert.IsTrue(acted);
            SwingsAtThePlayer(Times.Never());
            Assert.AreEqual((short)4, monster.PositionY, "Speed / 2 = 3 cells: (4,1) -> (4,4)");
        }

        [TestMethod]
        public async Task HostileMonsterIgnoresAPlayerBeyondItsNoticeRange()
        {
            var monster = SpawnMonster(4, 1, m => m.NoticeRange = 2);
            PlacePlayer(4, 5);

            var acted = await _ai.TickAsync(monster);

            Assert.IsFalse(acted);
            SwingsAtThePlayer(Times.Never());
            Assert.AreEqual((short)1, monster.PositionY);
        }

        [TestMethod]
        public async Task PeacefulMonsterIgnoresAPlayerUntilItIsThreatened()
        {
            var monster = SpawnMonster(4, 1, m => m.IsHostile = false);
            PlacePlayer(4, 2);

            Assert.IsFalse(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Never());

            _aggro.AddThreat(monster, _session.Character, 5);

            Assert.IsTrue(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Once());
        }

        [TestMethod]
        public async Task ASwingIsNotRepeatedBeforeItsCooldownElapses()
        {
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 2);

            await _ai.TickAsync(monster);
            await _ai.TickAsync(monster);
            SwingsAtThePlayer(Times.Once());

            World.Clock.Advance(Duration.FromMilliseconds(1000));
            await _ai.TickAsync(monster);
            SwingsAtThePlayer(Times.Exactly(2));
        }

        [TestMethod]
        public async Task ARefusedSwingLeavesTheCooldownAlone()
        {
            AcceptSwings(false);
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 2);

            await _ai.TickAsync(monster);
            await _ai.TickAsync(monster);

            SwingsAtThePlayer(Times.Exactly(2));
        }

        [TestMethod]
        public async Task AMonsterThatCannotWalkNeverChases()
        {
            var monster = SpawnMonster(4, 1, m => m.CanWalk = false);
            PlacePlayer(4, 5);
            _aggro.AddThreat(monster, _session.Character, 1);

            var acted = await _ai.TickAsync(monster);

            Assert.IsTrue(acted);
            Assert.AreEqual((short)1, monster.PositionY);
            SwingsAtThePlayer(Times.Never());
        }

        [TestMethod]
        public async Task ADeadTargetDropsTheAggroAndTheMonsterHeadsHome()
        {
            var monster = SpawnMonster(4, 1);
            monster.PositionY = 5;
            PlacePlayer(4, 5);
            _aggro.AddThreat(monster, _session.Character, 1);
            _session.Character.Hp = 0;
            _session.Character.IsAlive = false;

            var acted = await _ai.TickAsync(monster);

            Assert.IsTrue(acted);
            Assert.IsFalse(_aggro.Current(monster).HasTarget);
            Assert.AreEqual((short)2, monster.PositionY, "home is 4 cells away and Speed / 2 = 3 cells are walked");
            SwingsAtThePlayer(Times.Never());
        }

        [TestMethod]
        public async Task TheChasePathIsKeptOnTheMonsterAndDroppedWithItsTarget()
        {
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 6);
            await _ai.TickAsync(monster);

            Assert.AreEqual((short)6, monster.ChasePath?.TargetY);

            _session.Character.Hp = 0;
            _session.Character.IsAlive = false;
            monster.PositionX = monster.FirstX;
            monster.PositionY = monster.FirstY;
            await _ai.TickAsync(monster);

            Assert.IsNull(monster.ChasePath);
        }

        [TestMethod]
        public async Task AMonsterMovedOffItsCachedPathPlansAgain()
        {
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 6);
            await _ai.TickAsync(monster);
            monster.PositionY = 1;

            await _ai.TickAsync(monster);

            Assert.AreEqual((short)4, monster.PositionY, "walks 3 cells from where it stands, not on from the old path");
        }

        [TestMethod]
        public async Task AMonsterWithNoWayToItsTargetPausesBeforeSearchingAgain()
        {
            var searches = 0;
            var pathfinder = new Mock<IPathfinder>();
            pathfinder.Setup(p => p.FindPath(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns(() =>
                {
                    searches++;
                    return new List<(short, short)>();
                });
            var pathfinding = new Mock<IPathfindingService>();
            pathfinding.Setup(p => p.ForMap(It.IsAny<GameObject.Map.Map>())).Returns(pathfinder.Object);
            var ai = new MonsterAi(_battle.Object, _aggro, pathfinding.Object, World.SessionRegistry,
                World.DistanceCalculator, new Mock<INpcCombatCatalog>().Object,
                new Mock<IRandomProvider>().Object, World.Clock, new Dictionary<short, SkillDto>(),
                NullLogger<MonsterAi>.Instance);
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 5);

            for (var tick = 0; tick < 5; tick++)
            {
                await ai.TickAsync(monster);
            }

            Assert.AreEqual(1, searches);

            World.Clock.Advance(Duration.FromSeconds(3));
            await ai.TickAsync(monster);

            Assert.AreEqual(2, searches);
        }

        [TestMethod]
        public async Task AMonsterAwayFromHomeWalksBackAtItsChaseSpeed()
        {
            var monster = SpawnMonster(4, 1, m => m.IsHostile = false);
            monster.PositionY = 5;

            var acted = await _ai.TickAsync(monster);

            Assert.IsTrue(acted);
            Assert.AreEqual((short)2, monster.PositionY);
        }

        [TestMethod]
        public async Task AMonsterAtHomeWithNothingToDoLeavesTheWanderingToTheCaller()
        {
            var monster = SpawnMonster(4, 1, m => m.IsHostile = false);

            Assert.IsFalse(await _ai.TickAsync(monster));
        }

        [TestMethod]
        public async Task ADeadMonsterDoesNothing()
        {
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 2);
            monster.Hp = 0;
            monster.IsAlive = false;

            Assert.IsFalse(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Never());
        }

        [TestMethod]
        public async Task AGroupMemberJoinsTheFightOfAnotherMemberOfItsRace()
        {
            var (fighting, idle) = SpawnGroup(GroupAttackType.SameRace, m => m.Race = 2, m => m.Race = 2);
            PlacePlayer(4, 4);
            _aggro.AddThreat(fighting, _session.Character, 5);

            Assert.IsTrue(await _ai.TickAsync(idle));
            SwingsAtThePlayerFrom(idle, Times.Once());
        }

        [TestMethod]
        public async Task AMonsterOfAnotherRaceStaysOutOfTheFight()
        {
            var (fighting, idle) = SpawnGroup(GroupAttackType.SameRace, m => m.Race = 2, m => m.Race = 3);
            PlacePlayer(4, 4);
            _aggro.AddThreat(fighting, _session.Character, 5);

            Assert.IsFalse(await _ai.TickAsync(idle));
            SwingsAtThePlayerFrom(idle, Times.Never());
        }

        [TestMethod]
        public async Task AGroupMemberJoinsTheFightOfAnotherMemberWithItsIcon()
        {
            var (fighting, idle) = SpawnGroup(GroupAttackType.SameIcon, m => m.IconId = 7, m => m.IconId = 7);
            PlacePlayer(4, 4);
            _aggro.AddThreat(fighting, _session.Character, 5);

            Assert.IsTrue(await _ai.TickAsync(idle));
            SwingsAtThePlayerFrom(idle, Times.Once());
        }

        [TestMethod]
        public async Task AGroupMemberIgnoresAFightBeyondItsNoticeRange()
        {
            var (fighting, idle) = SpawnGroup(GroupAttackType.SameRace, m => m.Race = 2, m =>
            {
                m.Race = 2;
                m.NoticeRange = 1;
            });
            PlacePlayer(4, 2);
            _aggro.AddThreat(fighting, _session.Character, 5);

            Assert.IsFalse(await _ai.TickAsync(idle));
            SwingsAtThePlayerFrom(idle, Times.Never());
        }

        // The shared fixture map declares 8 rows but ships 6, which the pathfinder walks off.
        [TestMethod]
        public async Task AMonsterWindingUpASkillStandsStillWithoutHoldingUpTheLoop()
        {
            var catalog = new Mock<INpcCombatCatalog>();
            catalog.Setup(c => c.GetSkills(1)).Returns(new List<NpcMonsterSkillDto>
            {
                new() { NpcMonsterVNum = 1, SkillVNum = 50 }
            });
            var random = new Mock<IRandomProvider>();
            random.Setup(r => r.Next(0, 10)).Returns(9);
            var ai = new MonsterAi(_battle.Object, _aggro,
                World.Create<GameObject.Services.PathfindingService.PathfindingService>(), World.SessionRegistry,
                World.DistanceCalculator, catalog.Object, random.Object, World.Clock,
                new Dictionary<short, SkillDto> { [50] = new() { SkillVNum = 50, CastId = 3, Range = 2, Cooldown = 100, CastTime = 10 } },
                NullLogger<MonsterAi>.Instance);
            var cast = new TaskCompletionSource<bool>();
            _battle.Setup(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.Is<HitArguments>(a => a.SkillId == 3)))
                .Returns(cast.Task);
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 3);

            Assert.IsTrue(await ai.TickAsync(monster));
            PlacePlayer(4, 6);
            await ai.TickAsync(monster);

            Assert.AreEqual((short)1, monster.PositionY);
            _battle.Verify(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()), Times.Once);

            World.Clock.Advance(Duration.FromMilliseconds(1000));
            await ai.TickAsync(monster);
            cast.SetResult(true);

            Assert.AreNotEqual((short)1, monster.PositionY);
        }

        private static GameObject.Map.Map TestMap()
        {
            var data = new List<byte> { 8, 0, 8, 0 };
            for (var y = 0; y < 8; y++)
            {
                data.AddRange(y is >= 1 and <= 3
                    ? new byte[] { 0, 1, 1, 1, 0, 0, 0, 0 }
                    : new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            }

            return new GameObject.Map.Map { MapId = 0, NameI18NKey = "pathTestMap", Data = data.ToArray() };
        }

        private MonsterComponentBundle SpawnMonster(short x, short y, Action<NpcMonsterDto>? tune = null)
        {
            var template = new NpcMonsterDto
            {
                NpcMonsterVNum = 1,
                MaxHp = 100,
                MaxMp = 50,
                Level = 5,
                IsHostile = true,
                NoticeRange = 5,
                BasicRange = 1,
                BasicCooldown = 10,
                CanWalk = true,
                Speed = 6,
            };
            tune?.Invoke(template);
            _map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = x, MapY = y, VNum = 1 } },
                new List<NpcMonsterDto> { template });
            return _map.EnumerateMonsters().Single();
        }

        // Two peaceful monsters of one group: the first at (4,1), the second at (4,5).
        private (MonsterComponentBundle First, MonsterComponentBundle Second) SpawnGroup(GroupAttackType group,
            Action<NpcMonsterDto> tuneFirst, Action<NpcMonsterDto> tuneSecond)
        {
            NpcMonsterDto Template(short vnum, Action<NpcMonsterDto> tune)
            {
                var template = new NpcMonsterDto
                {
                    NpcMonsterVNum = vnum,
                    MaxHp = 100,
                    MaxMp = 50,
                    Level = 5,
                    IsHostile = false,
                    GroupAttack = (byte)group,
                    NoticeRange = 5,
                    BasicRange = 1,
                    BasicCooldown = 10,
                    CanWalk = true,
                    Speed = 6,
                };
                tune(template);
                return template;
            }

            _map.LoadMonsters(
                new List<MapMonsterDto>
                {
                    new() { MapMonsterId = 200, MapId = 0, MapX = 4, MapY = 1, VNum = 1 },
                    new() { MapMonsterId = 201, MapId = 0, MapX = 4, MapY = 5, VNum = 2 },
                },
                new List<NpcMonsterDto> { Template(1, tuneFirst), Template(2, tuneSecond) });
            var monsters = _map.EnumerateMonsters().ToList();
            return (monsters.Single(m => m.VisualId == 200), monsters.Single(m => m.VisualId == 201));
        }

        private void SwingsAtThePlayerFrom(MonsterComponentBundle monster, Times times)
        {
            var playerId = _session.Character.VisualId;
            var monsterId = monster.VisualId;
            _battle.Verify(b => b.Hit(It.Is<IAliveEntity>(m => m.VisualId == monsterId),
                It.Is<IAliveEntity>(t => t.VisualId == playerId), It.IsAny<HitArguments>()), times);
        }

        private void PlacePlayer(short x, short y)
        {
            _session.Character.PositionX = x;
            _session.Character.PositionY = y;
        }

        private void AcceptSwings(bool accepted) =>
            _battle.Setup(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(accepted);

        private void SwingsAtThePlayer(Times times)
        {
            var playerId = _session.Character.VisualId;
            _battle.Verify(b => b.Hit(It.IsAny<IAliveEntity>(), It.Is<IAliveEntity>(t => t.VisualId == playerId),
                It.Is<HitArguments>(a => a.SkillId == 0)), times);
        }
    }
}

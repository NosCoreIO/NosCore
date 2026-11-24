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
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.PathfindingService;
using NosCore.PathFinder.Interfaces;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Tests.Shared;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

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
        private Mock<IMonsterChatterService> _chatter;
        private Mock<IRandomProvider> _random;
        private Mock<INpcCombatCatalog> _catalog;
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
            _chatter = new Mock<IMonsterChatterService>();
            _random = new Mock<IRandomProvider>();
            _catalog = new Mock<INpcCombatCatalog>();
            _catalog.Setup(c => c.GetNpcBCards(It.IsAny<short>())).Returns(new List<BCardDto>());
            _ai = new MonsterAi(
                _battle.Object,
                _aggro,
                World.Create<GameObject.Services.PathfindingService.PathfindingService>(),
                World.SessionRegistry,
                World.DistanceCalculator,
                _catalog.Object,
                _random.Object,
                _chatter.Object,
                World.Clock,
                new NosCore.GameObject.Services.SkillService.SkillCatalog([]),
                NullLogger<MonsterAi>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
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
                new Mock<IMonsterChatterService>().Object,
                new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0)),
                new NosCore.GameObject.Services.SkillService.SkillCatalog([]),
                new Mock<ILogger<MonsterAi>>().Object, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);

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
        public async Task AFightingMonsterShoutsOnEveryTickItsChatterRollComesUp()
        {
            _random.Setup(r => r.Next(0, 50)).Returns(0);
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 5);

            await _ai.TickAsync(monster);
            await _ai.TickAsync(monster);

            ShoutsFrom(monster, Times.Exactly(2));
        }

        [TestMethod]
        public async Task AFightingMonsterStaysQuietWhileItsChatterRollMisses()
        {
            _random.Setup(r => r.Next(0, 50)).Returns(1);
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 5);

            await _ai.TickAsync(monster);

            Assert.IsTrue(_aggro.Current(monster).HasTarget);
            ShoutsFrom(monster, Times.Never());
        }

        [TestMethod]
        public async Task MonstersThatNoticeOnTheSameTickRollTheirChatterSeparately()
        {
            _random.SetupSequence(r => r.Next(0, 50)).Returns(0).Returns(17);
            var (first, second) = SpawnGroup(GroupAttackType.None, m => m.IsHostile = true, m => m.IsHostile = true);
            PlacePlayer(6, 3);

            await _ai.TickAsync(first);
            await _ai.TickAsync(second);

            Assert.IsTrue(_aggro.Current(first).HasTarget);
            Assert.IsTrue(_aggro.Current(second).HasTarget);
            ShoutsFrom(first, Times.Once());
            ShoutsFrom(second, Times.Never());
        }

        [TestMethod]
        public async Task AMonsterWithoutATargetNeverShouts()
        {
            _random.Setup(r => r.Next(0, 50)).Returns(0);
            var monster = SpawnMonster(4, 1, m => m.NoticeRange = 2);
            PlacePlayer(4, 5);

            await _ai.TickAsync(monster);

            _chatter.Verify(c => c.ShoutAsync(It.IsAny<INonPlayableEntity>()), Times.Never());
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
        public async Task HostileMonsterDoesNotNoticeAHiddenPlayer()
        {
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 2);
            _session.Character.Invisible = true;

            Assert.IsFalse(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Never());
        }

        [TestMethod]
        public async Task AMonsterLosesAPlayerWhoHides()
        {
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 2);
            _aggro.AddThreat(monster, _session.Character, 5);
            _session.Character.Invisible = true;

            await _ai.TickAsync(monster);

            Assert.IsFalse(_aggro.Current(monster).HasTarget);
            SwingsAtThePlayer(Times.Never());
        }

        [TestMethod]
        public async Task AMonsterThatSeesHiddenThingsStillAttacksAHiddenPlayer()
        {
            var catalog = new Mock<INpcCombatCatalog>();
            catalog.Setup(c => c.GetNpcBCards(It.IsAny<short>())).Returns(new List<BCardDto>
            {
                new()
                {
                    Type = BCardEffect.SpecialActionsSeeHiddenThings.Type(),
                    SubType = BCardEffect.SpecialActionsSeeHiddenThings.SubType()
                }
            });
            var ai = new MonsterAi(_battle.Object, _aggro,
                World.Create<GameObject.Services.PathfindingService.PathfindingService>(), World.SessionRegistry,
                World.DistanceCalculator, catalog.Object, new Mock<IRandomProvider>().Object, _chatter.Object, World.Clock,
                new NosCore.GameObject.Services.SkillService.SkillCatalog([]),
                NullLogger<MonsterAi>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
            var monster = SpawnMonster(4, 1);
            PlacePlayer(4, 2);
            _session.Character.Invisible = true;

            await ai.TickAsync(monster);

            SwingsAtThePlayer(Times.Once());
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
        public async Task AHostileMonsterSummonedPassiveIgnoresAPlayerUntilItIsThreatened()
        {
            var monster = _map.SummonMonster(Template(), 4, 1, isHostile: false);
            PlacePlayer(4, 2);

            Assert.IsFalse(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Never());

            _aggro.AddThreat(monster, _session.Character, 5);

            Assert.IsTrue(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Once());
        }

        [TestMethod]
        public async Task APeacefulMonsterSummonedHostileStaysPeaceful()
        {
            var monster = _map.SummonMonster(Template(m => m.IsHostile = false), 4, 1, isHostile: true);
            PlacePlayer(4, 2);

            Assert.IsFalse(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Never());
        }

        [TestMethod]
        public async Task AHostileNpcAttacksAMonsterWithinReach()
        {
            var monster = _map.SummonMonster(Template(m => m.IsHostile = false), 4, 1, isHostile: false);
            _map.LoadNpcs(
                new List<MapNpcDto> { new() { MapNpcId = 300, MapId = 0, MapX = 4, MapY = 2, VNum = 2 } },
                new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog(
                    new List<NpcMonsterDto> { Template(m => m.NpcMonsterVNum = 2) }));
            var guard = _map.EnumerateNpcs().Single();

            Assert.IsTrue(await _ai.TickAsync(guard));
            var monsterId = monster.VisualId;
            _battle.Verify(b => b.Hit(It.Is<IAliveEntity>(e => e.VisualType == VisualType.Npc),
                It.Is<IAliveEntity>(t => t.VisualType == VisualType.Monster && t.VisualId == monsterId),
                It.IsAny<HitArguments>()), Times.Once());
        }

        [TestMethod]
        public async Task AMonsterHitByANosMateFightsBack()
        {
            var monster = SpawnMonster(4, 1, m => m.IsHostile = false);
            PlacePlayer(4, 5);
            var pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[pet.MateTransportId] = pet;
            await pet.SpawnAsync(_session.Character, _map, 4, 2);

            _aggro.AddThreat(monster, pet.Entity.NotNull(), 5);

            Assert.IsTrue(await _ai.TickAsync(monster));
            var petId = pet.MateTransportId;
            _battle.Verify(b => b.Hit(It.IsAny<IAliveEntity>(), It.Is<IAliveEntity>(t => t.VisualId == petId),
                It.IsAny<HitArguments>()), Times.Once());
        }

        [TestMethod]
        public async Task AMonsterFightsBackAnNpcSharingItsId()
        {
            var monster = SpawnMonster(4, 1, m => m.IsHostile = false);
            _map.LoadNpcs(
                new List<MapNpcDto> { new() { MapNpcId = 200, MapId = 0, MapX = 4, MapY = 2, VNum = 2 } },
                new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog(
                    new List<NpcMonsterDto> { new() { NpcMonsterVNum = 2, MaxHp = 100, Level = 5 } }));
            var guard = _map.EnumerateNpcs().Single();

            _aggro.AddThreat(monster, guard, 5);

            Assert.IsTrue(await _ai.TickAsync(monster));
            _battle.Verify(b => b.Hit(It.IsAny<IAliveEntity>(),
                It.Is<IAliveEntity>(t => t.VisualType == VisualType.Npc && t.VisualId == 200),
                It.IsAny<HitArguments>()), Times.Once());
        }

        [TestMethod]
        [DataRow(true, 1)]
        [DataRow(false, 0)]
        public async Task AnNpcOfAPeacefulKindGoesForAMonsterOnlyAsAnAlly(bool ally, int swings)
        {
            var monster = SpawnMonster(4, 1, m => m.IsHostile = false);
            var npc = _map.SummonNpc(new NpcMonsterDto
            {
                NpcMonsterVNum = 2, MaxHp = 100, Level = 5, NoticeRange = 5, BasicRange = 1, BasicCooldown = 10,
                CanWalk = true, Speed = 6
            }, 4, 2);
            npc.IsHostile = ally;

            await _ai.TickAsync(npc);

            _battle.Verify(b => b.Hit(It.IsAny<IAliveEntity>(),
                It.Is<IAliveEntity>(t => t.VisualType == VisualType.Monster && t.VisualId == monster.VisualId),
                It.IsAny<HitArguments>()), Times.Exactly(swings));
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
        public async Task AMonsterWhoseTargetLeavesTurnsOnTheNextOneWhoHurtIt()
        {
            var monster = SpawnMonster(4, 1, m => m.IsHostile = false);
            PlacePlayer(4, 2);
            var leaver = await World.CreateSessionAsync();
            leaver.Character.MapInstance = _map;
            leaver.Character.PositionX = 5;
            leaver.Character.PositionY = 1;
            Hurt(monster, leaver.Character, 50);
            Hurt(monster, _session.Character, 25);
            Assert.AreEqual(leaver.Character.VisualId, _aggro.Current(monster).TargetVisualId, "the first one to hurt it keeps its attention");

            World.SessionRegistry.Unregister(leaver.ConnectedChannel.Id);

            Assert.IsTrue(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Once());
            Assert.AreEqual(_session.Character.VisualId, _aggro.Current(monster).TargetVisualId);
        }

        [TestMethod]
        public async Task AMonsterWhoseTargetLeavesForgetsSomeoneWhoHasNotHurtIt()
        {
            var monster = SpawnMonster(4, 1, m => m.IsHostile = false);
            PlacePlayer(4, 2);
            var leaver = await World.CreateSessionAsync();
            leaver.Character.MapInstance = _map;
            Hurt(monster, leaver.Character, 50);

            World.SessionRegistry.Unregister(leaver.ConnectedChannel.Id);

            Assert.IsFalse(await _ai.TickAsync(monster));
            SwingsAtThePlayer(Times.Never());
            Assert.IsFalse(_aggro.Current(monster).HasTarget);
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
                new Mock<IRandomProvider>().Object, _chatter.Object, World.Clock, new NosCore.GameObject.Services.SkillService.SkillCatalog([]),
                NullLogger<MonsterAi>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
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
        public async Task AMonsterWindingUpASkillStandsStillUntilItsHitResolvesWithoutHoldingUpTheLoop()
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
                World.DistanceCalculator, catalog.Object, random.Object, _chatter.Object, World.Clock,
                new NosCore.GameObject.Services.SkillService.SkillCatalog([new SkillDto { SkillVNum = 50, CastId = 3, Range = 2, Cooldown = 100, CastTime = 10 }]),
                NullLogger<MonsterAi>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
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
            Assert.AreEqual((short)1, monster.PositionY, "the hit has not resolved yet");

            cast.SetResult(true);
            await ai.TickAsync(monster);
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
            _map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = x, MapY = y, VNum = 1 } },
                new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog(new List<NpcMonsterDto> { Template(tune) }));
            return _map.EnumerateMonsters().Single();
        }

        private static NpcMonsterDto Template(Action<NpcMonsterDto>? tune = null)
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
            return template;
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
                new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog(new List<NpcMonsterDto> { Template(1, tuneFirst), Template(2, tuneSecond) }));
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

        private void ShoutsFrom(MonsterComponentBundle monster, Times times)
        {
            var monsterId = monster.VisualId;
            _chatter.Verify(c => c.ShoutAsync(It.Is<INonPlayableEntity>(e => e.VisualId == monsterId)), times);
        }

        private void PlacePlayer(short x, short y)
        {
            _session.Character.PositionX = x;
            _session.Character.PositionY = y;
        }

        private void Hurt(MonsterComponentBundle monster, IAliveEntity attacker, int damage)
        {
            monster.HitList[attacker.Handle] = damage;
            _aggro.AddThreat(monster, attacker, damage);
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

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using Wolverine;
using FakeEntity = NosCore.GameObject.Tests.Services.BattleService.BattleServiceTests.FakeEntity;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class AreaAndTargetListSkillTests
    {
        private ClientSession _caster = null!;
        private ClientSession _bystander = null!;
        private MapInstance _map = null!;
        private IReadOnlyList<MonsterComponentBundle> _monsters = null!;
        private IReadOnlyList<IAliveEntity> _resolved = null!;

        private Mock<ISkillResolver> _skillResolver = null!;
        private Mock<ITargetResolver> _targetResolver = null!;
        private Mock<IHitQueue> _hitQueue = null!;
        private GameObject.Services.BattleService.BattleService _service = null!;
        private readonly List<IAliveEntity> _hitEntities = new();
        private FakeEntity _origin = null!;
        private bool _accepted;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _caster = await TestHelpers.Instance.GenerateSessionAsync();
            _bystander = await TestHelpers.Instance.GenerateSessionAsync();
            _map = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            _caster.Character.MapInstance = _map;
            _bystander.Character.MapInstance = _map;

            _skillResolver = new Mock<ISkillResolver>();
            _targetResolver = new Mock<ITargetResolver>();
            _hitQueue = new Mock<IHitQueue>();
            _hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .Callback<HitRequest>(r => _hitEntities.Add(r.Target))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 10, SuPacketHitMode.SuccessAttack, false));
            _service = new GameObject.Services.BattleService.BattleService(
                _skillResolver.Object,
                _targetResolver.Object,
                _hitQueue.Object,
                new Mock<IMessageBus>().Object,
                new Mock<GameObject.Services.BroadcastService.ISessionRegistry>().Object,
                NodaTime.SystemClock.Instance,
                new Mock<ICaptureService>().Object,
                NullLogger<GameObject.Services.BattleService.BattleService>.Instance);
            _origin = new FakeEntity { Hp = 100, VisualId = 1 };
        }

        private static SkillInfo GroundSkill(byte targetRange) => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.ZoneHit, Range: 8, TargetRange: targetRange, TargetType: 0,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        [TestMethod]
        public async Task AGroundSkillHitsOnlyTheMonstersInsideItsArea()
        {
            await new Spec("A ground skill cast at a cell hits the monsters within its target range of that cell and no others")
                .Given(MonstersStandAt_, new[] { (10, 10), (12, 11), (14, 10), (10, 20) })
                .When(TheCasterResolvesAGroundSkillOfTargetRange_At_, (byte)2, (10, 10))
                .Then(TheMonstersHitShouldBe_, new[] { 0, 1 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroundSkillSkipsDeadMonsters()
        {
            await new Spec("A dead monster inside the area is not a target")
                .Given(MonstersStandAt_, new[] { (10, 10), (11, 10) })
                .And(Monster_IsDead, 1)
                .When(TheCasterResolvesAGroundSkillOfTargetRange_At_, (byte)2, (10, 10))
                .Then(TheMonstersHitShouldBe_, new[] { 0 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroundSkillNeverHitsAFriendlyPlayer()
        {
            await new Spec("A player standing in a player's ground skill is not hit, and neither is the caster")
                .Given(MonstersStandAt_, new[] { (10, 10) })
                .And(TheBystanderStandsAt_, (11, 10))
                .And(TheCasterStandsAt_, (10, 11))
                .When(TheCasterResolvesAGroundSkillOfTargetRange_At_, (byte)2, (10, 10))
                .Then(NoPlayerShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAreaCastHitsEveryResolvedTarget()
        {
            await new Spec("An area cast sends one hit per entity in the area and is accepted")
                .Given(TheSkillResolves)
                .And(TheAreaHolds_Targets, 3)
                .WhenAsync(CastingAtACell)
                .Then(TheCastShouldBeAccepted_, true)
                .And(_HitsShouldBeSent, 3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAreaCastOnEmptyGroundIsStillACast()
        {
            await new Spec("A ground skill that lands on nobody is still accepted, so it costs its mana and cooldown")
                .Given(TheSkillResolves)
                .And(TheAreaHolds_Targets, 0)
                .WhenAsync(CastingAtACell)
                .Then(TheCastShouldBeAccepted_, true)
                .And(_HitsShouldBeSent, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadCasterCannotCastAtACell()
        {
            await new Spec("A dead caster's area cast is refused before anything is hit")
                .Given(TheSkillResolves)
                .And(TheAreaHolds_Targets, 2)
                .And(TheCasterIsDead)
                .WhenAsync(CastingAtACell)
                .Then(TheCastShouldBeAccepted_, false)
                .And(_HitsShouldBeSent, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATargetListHitsEachValidTargetOnce()
        {
            await new Spec("Each listed target is hit once, even when the client lists it twice")
                .Given(TheSkillResolves)
                .WhenAsync(CastingOn_, new[] { Monster(1), Monster(2), Monster(2) })
                .Then(TheCastShouldBeAccepted_, true)
                .And(TheTargetsHitShouldBe_, new long[] { 1, 2 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATargetListSkipsDeadAndFriendlyTargets()
        {
            await new Spec("Dead monsters, players and targets on another map are dropped from the list")
                .Given(TheSkillResolves)
                .WhenAsync(CastingOn_, new[] { Monster(1), Monster(2, hp: 0), Player(3), Monster(4, onOtherMap: true) })
                .Then(TheCastShouldBeAccepted_, true)
                .And(TheTargetsHitShouldBe_, new long[] { 1 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATargetListWithNothingValidIsRefused()
        {
            await new Spec("A list with no target left is refused, so it costs nothing")
                .Given(TheSkillResolves)
                .WhenAsync(CastingOn_, new[] { Monster(2, hp: 0), Player(3) })
                .Then(TheCastShouldBeAccepted_, false)
                .And(_HitsShouldBeSent, 0)
                .ExecuteAsync();
        }

        private void MonstersStandAt_((int X, int Y)[] cells)
        {
            var template = new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 };
            _map.LoadMonsters(
                cells.Select((cell, i) => new MapMonsterDto { MapMonsterId = 200 + i, MapId = 0, MapX = (short)cell.X, MapY = (short)cell.Y, VNum = 1 }).ToList(),
                new List<NpcMonsterDto> { template });
            _monsters = _map.Monsters.OrderBy(m => m.VisualId).ToList();
        }

        private void Monster_IsDead(int index)
        {
            var monster = _monsters[index];
            monster.Hp = 0;
            monster.IsAlive = false;
        }

        private void TheBystanderStandsAt_((int X, int Y) cell)
        {
            _bystander.Character.PositionX = (short)cell.X;
            _bystander.Character.PositionY = (short)cell.Y;
        }

        private void TheCasterStandsAt_((int X, int Y) cell)
        {
            _caster.Character.PositionX = (short)cell.X;
            _caster.Character.PositionY = (short)cell.Y;
        }

        private void TheCasterResolvesAGroundSkillOfTargetRange_At_(byte targetRange, (int X, int Y) cell)
        {
            _resolved = new TargetResolver(TestHelpers.Instance.SessionRegistry)
                .ResolveArea(_caster.Character, _map, (short)cell.X, (short)cell.Y, GroundSkill(targetRange));
        }

        private void TheMonstersHitShouldBe_(int[] indexes)
        {
            CollectionAssert.AreEquivalent(
                indexes.Select(i => _monsters[i].VisualId).ToArray(),
                _resolved.Where(t => t.VisualType == VisualType.Monster).Select(t => t.VisualId).ToArray());
        }

        private void NoPlayerShouldBeHit()
        {
            Assert.IsFalse(_resolved.Any(t => t.VisualType == VisualType.Player));
        }

        private void TheSkillResolves()
        {
            _origin.MapInstance = _map;
            _skillResolver.Setup(r => r.Resolve(_origin, 1L)).Returns(GroundSkill(2));
        }

        private void TheAreaHolds_Targets(int count)
        {
            var targets = Enumerable.Range(0, count)
                .Select(i => (IAliveEntity)new FakeEntity { Hp = 100, VisualId = 10 + i, VisualType = VisualType.Monster })
                .ToList();
            _targetResolver.Setup(r => r.ResolveArea(_origin, _map, 10, 10, It.IsAny<SkillInfo>())).Returns(targets);
        }

        private void TheCasterIsDead()
        {
            _origin.Hp = 0;
        }

        private async Task CastingAtACell()
        {
            _accepted = await _service.HitArea(_origin, 10, 10, new HitArguments { SkillId = 1 });
        }

        private FakeEntity Monster(long id, int hp = 100, bool onOtherMap = false) => new()
        {
            Hp = hp,
            VisualId = id,
            VisualType = VisualType.Monster,
            MapInstance = onOtherMap ? null! : _map,
        };

        private FakeEntity Player(long id) => new()
        {
            Hp = 100,
            VisualId = id,
            VisualType = VisualType.Player,
            MapInstance = _map,
        };

        private async Task CastingOn_(FakeEntity[] targets)
        {
            _accepted = await _service.HitTargets(_origin, targets, new HitArguments { SkillId = 1 });
        }

        private void TheCastShouldBeAccepted_(bool accepted)
        {
            Assert.AreEqual(accepted, _accepted);
        }

        private void _HitsShouldBeSent(int count)
        {
            Assert.HasCount(count, _hitEntities);
        }

        private void TheTargetsHitShouldBe_(long[] ids)
        {
            CollectionAssert.AreEqual(ids, _hitEntities.Select(t => t.VisualId).ToArray());
        }
    }
}

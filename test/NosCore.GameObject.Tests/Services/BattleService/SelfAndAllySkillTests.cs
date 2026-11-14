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
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using SpecLight;
using Wolverine;
using FakeEntity = NosCore.GameObject.Tests.Services.BattleService.BattleServiceTests.FakeEntity;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SelfAndAllySkillTests
    {
        private static readonly IReadOnlyList<BCardDto> Cards = new[] { new BCardDto { Type = 25, SubType = 11, FirstData = 400, SecondData = 13 } };

        private TestWorld World;
        private ClientSession _caster;
        private ClientSession _ally;
        private MapInstance _map;
        private Mock<ISkillResolver> _skillResolver;
        private Mock<ITargetResolver> _targetResolver;
        private Mock<IHitQueue> _hitQueue;
        private Mock<IBuffService> _buffService;
        private GameObject.Services.BattleService.BattleService _service;
        private readonly List<IAliveEntity> _hitEntities = new();
        private bool _accepted;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _caster = await World.CreateSessionAsync();
            _ally = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            _caster.Character.MapInstance = _map;
            _ally.Character.MapInstance = _map;
            _caster.Character.PositionX = 10;
            _caster.Character.PositionY = 10;
            _ally.Character.PositionX = 12;
            _ally.Character.PositionY = 10;

            _skillResolver = new Mock<ISkillResolver>();
            _targetResolver = new Mock<ITargetResolver>();
            _hitQueue = new Mock<IHitQueue>();
            _hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .Callback<HitRequest>(r => _hitEntities.Add(r.Target))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 10, SuPacketHitMode.SuccessAttack, false));
            _buffService = new Mock<IBuffService>();
            _buffService.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(Array.Empty<BuffInstance>());
            _service = new GameObject.Services.BattleService.BattleService(
                _skillResolver.Object,
                _targetResolver.Object,
                _hitQueue.Object,
                new Mock<IMessageBus>().Object,
                World.SessionRegistry,
                NodaTime.SystemClock.Instance,
                new Mock<ICaptureService>().Object,
                new Mock<ICastDelayService>().Object,
                _buffService.Object,
                new SkillCastService(NodaTime.SystemClock.Instance, new Mock<GameObject.Services.EquipmentService.IEquipmentStatsService> { DefaultValue = DefaultValue.Empty }.Object, _buffService.Object),
                NullLogger<GameObject.Services.BattleService.BattleService>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task ASelfBuffLandsOnTheCaster()
        {
            await new Spec("A skill cast on yourself is accepted and gives you its cards, without a blow")
                .Given(TheSkillTargets_WithHitType_, (byte)1, TargetHitType.ZoneHit)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCastShouldBeAccepted_, true)
                .And(TheCardsShouldReach_, new[] { "caster" })
                .And(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAllyBuffLandsOnTheChosenPlayer()
        {
            await new Spec("A skill cast on a friendly player gives that player its cards")
                .Given(TheSkillTargets_WithHitType_, (byte)2, TargetHitType.SingleTargetHit)
                .WhenAsync(TheCasterCastsItOn_, "ally")
                .Then(TheCastShouldBeAccepted_, true)
                .And(TheCardsShouldReach_, new[] { "ally" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAllyBuffCannotBeCastOnAMonster()
        {
            await new Spec("A skill meant for a friendly player is refused on a monster")
                .Given(TheSkillTargets_WithHitType_, (byte)2, TargetHitType.SingleTargetHit)
                .WhenAsync(TheCasterCastsItOn_, "monster")
                .Then(TheCastShouldBeAccepted_, false)
                .And(TheCardsShouldReach_, Array.Empty<string>())
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroupBuffLandsOnTheFriendlyPlayersAround()
        {
            await new Spec("A skill buffing everyone around reaches the caster and the friendly players in its range")
                .Given(TheSkillTargets_WithHitType_, (byte)1, TargetHitType.SingleAoeTargetHit)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCardsShouldReach_, new[] { "caster", "ally" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroupBuffLeavesOutAPlayerOutOfRange()
        {
            await new Spec("A friendly player outside the range is not buffed")
                .Given(TheSkillTargets_WithHitType_, (byte)1, TargetHitType.SingleAoeTargetHit)
                .And(TheAllyWalksAway)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCardsShouldReach_, new[] { "caster" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAttackAroundTheCasterHitsTheEnemiesAround()
        {
            await new Spec("A skill hitting around the caster strikes each enemy in its area and buffs nobody")
                .Given(TheSkillTargets_WithHitType_, (byte)1, TargetHitType.SingleTargetHitCombo)
                .And(TwoMonstersStandAroundTheCaster)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCastShouldBeAccepted_, true)
                .And(_HitsShouldBeSent, 2)
                .And(TheCardsShouldReach_, Array.Empty<string>())
                .ExecuteAsync();
        }

        private void TheSkillTargets_WithHitType_(byte targetType, TargetHitType hitType)
        {
            IAliveEntity caster = _caster.Character;
            _skillResolver.Setup(r => r.Resolve(caster, 1L)).Returns(new SkillInfo(
                SkillVnum: 207, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
                Type: 0, HitType: hitType, Range: 0, TargetRange: 4, TargetType: targetType,
                Element: 0, Duration: 0, MpCost: 0, BCards: Cards));
        }

        private void TheAllyWalksAway()
        {
            _ally.Character.PositionX = 30;
        }

        private void TwoMonstersStandAroundTheCaster()
        {
            var monsters = new List<IAliveEntity>
            {
                new FakeEntity { Hp = 100, VisualId = 50, VisualType = VisualType.Monster, MapInstance = _map },
                new FakeEntity { Hp = 100, VisualId = 51, VisualType = VisualType.Monster, MapInstance = _map },
            };
            IAliveEntity caster = _caster.Character;
            _targetResolver.Setup(r => r.ResolveArea(caster, _map, 10, 10, It.IsAny<SkillInfo>())).Returns(monsters);
        }

        private async Task TheCasterCastsItOn_(string who)
        {
            IAliveEntity target = who switch
            {
                "caster" => _caster.Character,
                "ally" => _ally.Character,
                _ => new FakeEntity { Hp = 100, VisualId = 50, VisualType = VisualType.Monster, MapInstance = _map },
            };
            _accepted = await _service.Hit(_caster.Character, target, new HitArguments { SkillId = 1 });
        }

        private void TheCastShouldBeAccepted_(bool accepted) => Assert.AreEqual(accepted, _accepted);

        private void TheCardsShouldReach_(string[] who)
        {
            var expected = who.Select(w => w == "caster" ? _caster.Character.VisualId : _ally.Character.VisualId).ToList();
            var reached = _buffService.Invocations
                .Where(i => i.Method.Name == nameof(IBuffService.InflictCardsAsync))
                .Select(i => i.Arguments[0] is IAliveEntity entity ? entity.VisualId : -1)
                .ToList();
            CollectionAssert.AreEquivalent(expected, reached);
        }

        private void NothingShouldBeHit() => Assert.AreEqual(0, _hitEntities.Count);

        private void _HitsShouldBeSent(int count) => Assert.AreEqual(count, _hitEntities.Count);
    }
}

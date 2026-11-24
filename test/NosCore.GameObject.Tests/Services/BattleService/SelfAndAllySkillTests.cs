//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Buff;
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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using SpecLight;
using FakeEntity = NosCore.GameObject.Tests.Services.BattleService.BattleServiceTests.FakeEntity;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SelfAndAllySkillTests
    {
        private static readonly IReadOnlyList<BCardDto> Cards = new[] { new BCardDto { Type = BCardType.CardType.Buff, SubType = 11, FirstData = 400, SecondData = 13 } };

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
                new Mock<IGameEventDispatcher>().Object,
                World.SessionRegistry,
                World.Clock,
                new Mock<ICaptureService>().Object,
                new Mock<ICastDelayService>().Object,
                _buffService.Object,
                new SkillCastService(World.Clock, new Mock<GameObject.Services.EquipmentService.IEquipmentStatsService> { DefaultValue = DefaultValue.Empty }.Object, _buffService.Object),
                new Mock<NosCore.GameObject.Services.BattleService.ISkillComboService>().Object,
                new Mock<ISummonService>().Object,
                new Mock<IOnyxDragonService>().Object,
                new Mock<IBattleStatsProvider>().Object,
                new Mock<IRandomProvider>().Object,
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
        public async Task AMonstersSupportSkillGoesToItAndTheMonstersAroundIt()
        {
            await new Spec("A monster's self skill reaching around it buffs it and the monsters near it, not the player it fights")
                .Given(AMonsterAt_, (short)10, (short)12)
                .And(AMonsterAt_, (short)12, (short)12)
                .And(AMonsterAt_, (short)30, (short)30)
                .And(TheMonstersSkillTargets_WithHitType_, TargetHitType.SingleAoeTargetHit, Cards)
                .WhenAsync(TheFirstMonsterCastsItOnTheCasterAsync)
                .Then(TheCastShouldBeAccepted_, true)
                .And(TheCardsShouldReachMonsters_, new[] { 0, 1 })
                .And(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMonstersSelfBuffGoesToItAlone()
        {
            await new Spec("A monster's self skill that only hands out cards buffs the monster alone")
                .Given(AMonsterAt_, (short)10, (short)12)
                .And(AMonsterAt_, (short)12, (short)12)
                .And(TheMonstersSkillTargets_WithHitType_, TargetHitType.SingleTargetHit, Cards)
                .WhenAsync(TheFirstMonsterCastsItOnTheCasterAsync)
                .Then(TheCardsShouldReachMonsters_, new[] { 0 })
                .And(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMonstersOtherSelfSkillStillStrikes()
        {
            await new Spec("A monster's self skill that does more than hand out cards still strikes its target")
                .Given(AMonsterAt_, (short)10, (short)12)
                .And(TheMonstersSkillTargets_WithHitType_, TargetHitType.SingleTargetHit,
                    (IReadOnlyList<BCardDto>)[new BCardDto { Type = BCardType.CardType.FearSkill, SubType = 11 }])
                .WhenAsync(TheFirstMonsterCastsItOnTheCasterAsync)
                .Then(_HitsShouldBeSent, 1)
                .ExecuteAsync();
        }

        private readonly List<MonsterComponentBundle> _monsters = new();
        private readonly List<NosCore.Data.Dto.MapMonsterDto> _spawns = new();
        private SkillDto _monsterSkill;

        private void AMonsterAt_(short x, short y)
        {
            _spawns.Add(new NosCore.Data.Dto.MapMonsterDto { MapMonsterId = 9001 + _spawns.Count, MapId = 0, MapX = x, MapY = y, VNum = 333 });
            _map.LoadMonsters(_spawns, new NpcMonsterCatalog(new List<NpcMonsterDto>
            {
                new() { NpcMonsterVNum = 333, Level = 10, MaxHp = 1000, MaxMp = 10, IsHostile = true },
            }));
            _monsters.Clear();
            _monsters.AddRange(_spawns.Select(spawn => _map.EnumerateMonsters().Single(m => m.VisualId == spawn.MapMonsterId)));
        }

        private void TheMonstersSkillTargets_WithHitType_(TargetHitType hitType, IReadOnlyList<BCardDto> cards)
        {
            _monsterSkill = new SkillDto { SkillVNum = 567, CastId = 0, TargetType = 1, HitType = (byte)hitType, TargetRange = 4 };
            _skillResolver.Setup(r => r.Describe(_monsterSkill)).Returns(new SkillInfo(
                SkillVnum: 567, CastId: 0, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
                Type: 0, HitType: hitType, Range: 2, TargetRange: 4, TargetType: 1,
                Element: 0, Duration: 0, MpCost: 0, BCards: cards));
            IAliveEntity monster = _monsters[0];
            IAliveEntity player = _caster.Character;
            _targetResolver.Setup(r => r.Resolve(monster, player, It.IsAny<SkillInfo>())).Returns(new[] { player });
        }

        private async Task TheFirstMonsterCastsItOnTheCasterAsync() =>
            _accepted = await _service.Hit(_monsters[0], _caster.Character, new HitArguments { Skill = _monsterSkill });

        private void TheCardsShouldReachMonsters_(int[] which)
        {
            var reached = _buffService.Invocations
                .Where(i => i.Method.Name == nameof(IBuffService.InflictCardsAsync))
                .Select(i => i.Arguments[0] is IAliveEntity e ? (e.VisualType, e.VisualId) : (VisualType.Map, -1L))
                .ToList();
            CollectionAssert.AreEquivalent(which.Select(i => (VisualType.Monster, _monsters[i].VisualId)).ToList(), reached);
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
        public async Task AGroupBuffLandsOnThePetsOutAround()
        {
            await new Spec("A skill buffing everyone around also reaches the pets the caster and the ally have out")
                .Given(TheSkillTargets_WithHitType_, (byte)1, TargetHitType.SingleAoeTargetHit)
                .AndAsync(TheCasterAndTheAllyHaveAPetOutAt_, (short)11)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCardsShouldReach_, new[] { "caster", "ally", "caster's pet", "ally's pet" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroupBuffLeavesOutAPetOutOfRange()
        {
            await new Spec("A pet outside the range is not buffed")
                .Given(TheSkillTargets_WithHitType_, (byte)1, TargetHitType.SingleAoeTargetHit)
                .AndAsync(TheCasterAndTheAllyHaveAPetOutAt_, (short)30)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCardsShouldReach_, new[] { "caster", "ally" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroupBuffShowsTheSkillLandingOnEachFriendAround()
        {
            await new Spec("Every friend a group buff reaches besides the caster sees the skill land on them")
                .Given(TheSkillTargets_WithHitType_, (byte)1, TargetHitType.SingleAoeTargetHit)
                .AndAsync(TheCasterAndTheAllyHaveAPetOutAt_, (short)11)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheSkillShouldBeShownLandingOn_, new[] { "ally", "caster's pet", "ally's pet" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroupBuffRefreshesTheBarsOfEachPetItReaches()
        {
            await new Spec("Each owner gets their buffed pet's bars again, so the new buff shows under it")
                .Given(TheSkillTargets_WithHitType_, (byte)1, TargetHitType.SingleAoeTargetHit)
                .AndAsync(TheCasterAndTheAllyHaveAPetOutAt_, (short)11)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(_ShouldGetTheBarsOf_, "caster", "caster's pet")
                .And(_ShouldGetTheBarsOf_, "ally", "ally's pet")
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

        [TestMethod]
        public async Task AnAttackAroundAHiddenCasterCarriesItsAmbushAndRevealsTheCaster()
        {
            await new Spec("A hidden caster striking all around adds the skill's ambush damage to every blow, then shows up")
                .Given(TheSkillAroundTheCasterCarriesAnAmbushOf_, 300)
                .And(TwoMonstersStandAroundTheCaster)
                .And(TheCasterIsHidden)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(EveryBlowShouldCarryAnAmbushOf_, 300)
                .And(TheCasterShouldBeRevealed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetsBasicAttackStrikesTheMonster()
        {
            await new Spec("A pet with no skill of its own still lands its basic attack on a monster")
                .GivenAsync(ThePetOfTheCasterOnlyHasABasicAttack)
                .WhenAsync(ThePetAttacksAMonster)
                .Then(TheCastShouldBeAccepted_, true)
                .And(_HitsShouldBeSent, 1)
                .ExecuteAsync();
        }

        private MateComponentBundle _pet;
        private readonly Dictionary<string, long> _pets = new();

        private async Task TheCasterAndTheAllyHaveAPetOutAt_(short x)
        {
            foreach (var (name, owner) in new[] { ("caster's pet", _caster), ("ally's pet", _ally) })
            {
                var pet = TestMates.Pet(owner.Character.CharacterId);
                owner.Character.Mates[pet.MateTransportId] = pet;
                await pet.SpawnAsync(owner.Character, _map, x, 10);
                _pets[name] = pet.MateTransportId;
            }

            _map.LastPackets.Clear();
        }

        private long VisualIdOf(string who) => who switch
        {
            "caster" => _caster.Character.VisualId,
            "ally" => _ally.Character.VisualId,
            _ => _pets[who],
        };

        private void _ShouldGetTheBarsOf_(string owner, string pet)
        {
            var session = owner == "caster" ? _caster : _ally;
            Assert.IsTrue(session.LastPackets.OfType<PstPacket>().Any(p => p.VisualId == _pets[pet]));
        }

        private void TheSkillShouldBeShownLandingOn_(string[] who)
        {
            var landed = _map.LastPackets.OfType<SuPacket>()
                .Where(su => su.HitMode == SuPacketHitMode.AttackedInAoe)
                .Select(su => su.TargetId)
                .ToList();
            CollectionAssert.AreEquivalent(who.Select(VisualIdOf).ToList(), landed);
        }

        private async Task ThePetOfTheCasterOnlyHasABasicAttack()
        {
            var pet = TestMates.Pet(_caster.Character.CharacterId);
            await pet.SpawnAsync(_caster.Character, _map, 11, 10);
            _pet = pet.Entity ?? throw new InvalidOperationException();
            IAliveEntity attacker = _pet;
            _skillResolver.Setup(r => r.Resolve(attacker, It.IsAny<long>())).Returns(new SkillInfo(
                SkillVnum: 0, CastId: 0, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
                Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 1, TargetRange: 0, TargetType: 1,
                Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>()));
        }

        private async Task ThePetAttacksAMonster()
        {
            IAliveEntity monster = new FakeEntity { Hp = 100, VisualId = 50, VisualType = VisualType.Monster, MapInstance = _map };
            IAliveEntity attacker = _pet;
            _targetResolver.Setup(r => r.Resolve(attacker, monster, It.IsAny<SkillInfo>())).Returns(new[] { monster });
            _accepted = await _service.Hit(_pet, monster, new HitArguments());
        }

        [TestMethod]
        public async Task ATeleportSkillJumpsItsRadiusTheWayTheCasterFaces()
        {
            await new Spec("A teleport skill moves the caster its radius in the direction it faces")
                .Given(TheCasterStandsAt_Facing_, (short)0, (short)7, (byte)1)
                .And(TheSkillTeleportsWithinARadiusOf_, 5)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCasterShouldStandAt_, (short)5, (short)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADiagonalTeleportCountsEachCellAsTheDiagonalOfOne()
        {
            await new Spec("Facing a diagonal, a radius of 8 fields is 6 cells")
                .Given(TheCasterStandsAt_Facing_, (short)0, (short)7, (byte)5)
                .And(TheSkillTeleportsWithinARadiusOf_, 8)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCasterShouldStandAt_, (short)6, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATeleportPassesOverAWall()
        {
            await new Spec("A teleport lands beyond a wall in its way")
                .Given(TheCasterStandsAt_Facing_, (short)0, (short)2, (byte)1)
                .And(TheSkillTeleportsWithinARadiusOf_, 4)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCasterShouldStandAt_, (short)4, (short)2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATeleportStopsShortOfAWallOrTheMapEdge()
        {
            await new Spec("A teleport whose landing cells are walls or off the map lands on the farthest walkable one")
                .Given(TheCasterStandsAt_Facing_, (short)0, (short)2, (byte)1)
                .And(TheSkillTeleportsWithinARadiusOf_, 3)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCasterShouldStandAt_, (short)0, (short)2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATeleportNeverLeavesTheMap()
        {
            await new Spec("A teleport towards the edge of the map stops at the edge")
                .Given(TheCasterStandsAt_Facing_, (short)2, (short)7, (byte)3)
                .And(TheSkillTeleportsWithinARadiusOf_, 8)
                .WhenAsync(TheCasterCastsItOn_, "caster")
                .Then(TheCasterShouldStandAt_, (short)0, (short)7)
                .ExecuteAsync();
        }

        private void TheCasterStandsAt_Facing_(short x, short y, byte direction)
        {
            _caster.Character.PositionX = x;
            _caster.Character.PositionY = y;
            _caster.Character.Direction = direction;
        }

        private void TheSkillTeleportsWithinARadiusOf_(int radius)
        {
            IAliveEntity caster = _caster.Character;
            _skillResolver.Setup(r => r.Resolve(caster, 1L)).Returns(new SkillInfo(
                SkillVnum: 277, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
                Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0, TargetType: 1,
                Element: 0, Duration: 0, MpCost: 0, BCards: new[]
                {
                    new BCardDto
                    {
                        Type = BCardEffect.SpecialEffects2TeleportInRadius.Type(),
                        SubType = BCardEffect.SpecialEffects2TeleportInRadius.SubType(),
                        FirstData = radius,
                    },
                }));
        }

        private void TheCasterShouldStandAt_(short x, short y)
        {
            Assert.AreEqual(x, _caster.Character.PositionX);
            Assert.AreEqual(y, _caster.Character.PositionY);
        }

        private void TheSkillTargets_WithHitType_(byte targetType, TargetHitType hitType)
        {
            IAliveEntity caster = _caster.Character;
            _skillResolver.Setup(r => r.Resolve(caster, 1L)).Returns(new SkillInfo(
                SkillVnum: 207, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
                Type: 0, HitType: hitType, Range: 0, TargetRange: 4, TargetType: targetType,
                Element: 0, Duration: 0, MpCost: 0, BCards: Cards));
        }

        private void TheSkillAroundTheCasterCarriesAnAmbushOf_(int damage)
        {
            IAliveEntity caster = _caster.Character;
            _skillResolver.Setup(r => r.Resolve(caster, 1L)).Returns(new SkillInfo(
                SkillVnum: 207, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
                Type: 0, HitType: TargetHitType.SingleTargetHitCombo, Range: 0, TargetRange: 4, TargetType: 1,
                Element: 0, Duration: 0, MpCost: 0, BCards: new[]
                {
                    new BCardDto
                    {
                        Type = BCardEffect.LightAndShadowAdditionalDamageWhenHidden.Type(),
                        SubType = BCardEffect.LightAndShadowAdditionalDamageWhenHidden.SubType(),
                        FirstData = (short)damage,
                    },
                }));
        }

        private void TheCasterIsHidden()
        {
            IAliveEntity caster = _caster.Character;
            _buffService.Setup(b => b.HasEffect(caster, BCardEffect.SpecialActionsHide)).Returns(true);
        }

        private void EveryBlowShouldCarryAnAmbushOf_(int damage)
        {
            _hitQueue.Verify(q => q.EnqueueAsync(It.Is<HitRequest>(h => h.Skill.AmbushDamage == damage)), Times.Exactly(2));
            _hitQueue.Verify(q => q.EnqueueAsync(It.Is<HitRequest>(h => h.Skill.AmbushDamage != damage)), Times.Never);
        }

        private void TheCasterShouldBeRevealed()
        {
            IAliveEntity caster = _caster.Character;
            _buffService.Verify(b => b.RemoveEffectAsync(caster, BCardEffect.SpecialActionsHide), Times.Once);
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
            var expected = who.Select(VisualIdOf).ToList();
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

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
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class ExplosionTests
    {
        private const short SmallWiggler = 229;
        private const int Radius = 2;
        private const int ExplosionDamage = 500;

        private TestWorld World;
        private MapInstance _map;
        private ClientSession _target;
        private ClientSession _nearby;
        private ClientSession _farAway;
        private Mock<IGameEventDispatcher> _gameEvents;
        private readonly List<HitRequest> _hits = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _map = World.GetBaseMap(TestWorld.TestMapId);
            _target = await PlayerAtAsync(10, 10);
            _nearby = await PlayerAtAsync(10, 13);
            _farAway = await PlayerAtAsync(10, 14);

            World.UseReal<ITargetResolver, TargetResolver>();
            World.Freeze<Mock<ISkillResolver>>()
                .Setup(r => r.ResolveBasic(It.IsAny<IAliveEntity>()))
                .Returns(new SkillInfo(0, 0, 12, 11, -1, 0, 0, Data.Enumerations.Battle.TargetHitType.SingleTargetHit,
                    1, 0, 0, 0, 0, 0, []));
            World.Freeze<Mock<IHitQueue>>()
                .Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .Callback<HitRequest>(_hits.Add)
                .Returns<HitRequest>(h => Task.FromResult(new HitOutcome(HitStatus.Landed, h.Skill.FixedDamage ?? 10,
                    SuPacketHitMode.SuccessAttack, false)));
            _gameEvents = World.Freeze<Mock<IGameEventDispatcher>>();
        }

        private async Task<ClientSession> PlayerAtAsync(short x, short y)
        {
            var session = await World.CreateSessionAsync();
            session.Character.MapInstance = _map;
            session.Character.PositionX = x;
            session.Character.PositionY = y;
            return session;
        }

        private MonsterComponentBundle Spawn(short x, short y, bool disappearsAfterHitting = false) =>
            _map.SpawnMonster(new NpcMonsterDto
            {
                NpcMonsterVNum = SmallWiggler, MaxHp = 100, BasicRange = 1, BasicCooldown = 12,
                DisappearAfterHitting = disappearsAfterHitting,
            }, x, y, false);

        private void Explodes(MonsterComponentBundle monster) =>
            World.Freeze<Mock<IBattleStatsProvider>>()
                .Setup(s => s.GetStats(It.IsAny<IAliveEntity>()))
                .Returns<IAliveEntity>(e => e.VisualType == VisualType.Monster && e.VisualId == monster.VisualId
                    ? new CombatStats { ExplosionRadius = Radius, ExplosionDamage = ExplosionDamage }
                    : new CombatStats());

        private Task<bool> SwingsAtTheTargetAsync(MonsterComponentBundle monster) =>
            World.Create<GameObject.Services.BattleService.BattleService>()
                .Hit(monster, _target.Character, new HitArguments { BasicAttack = true });

        [TestMethod]
        public async Task AnExplosionHitsEveryPlayerWithinItsRadiusForItsDamage()
        {
            var wiggler = Spawn(10, 11);
            Explodes(wiggler);

            await SwingsAtTheTargetAsync(wiggler);

            var blasts = _map.LastPackets.OfType<SuPacket>().Where(su => su.HitMode == SuPacketHitMode.AttackedInAoe).ToList();
            CollectionAssert.AreEquivalent(new[] { _target.Character.VisualId, _nearby.Character.VisualId },
                blasts.Select(su => su.TargetId).ToArray());
            var nearby = _nearby.Character;
            PacketAssert.AreEqual(new SuPacket
            {
                VisualType = VisualType.Monster,
                VisualId = wiggler.VisualId,
                TargetVisualType = VisualType.Player,
                TargetId = nearby.VisualId,
                SkillVnum = 0,
                SkillCooldown = 12,
                AttackAnimation = 11,
                SkillEffect = 0,
                TargetIsAlive = true,
                HpPercentage = (byte)(nearby.Hp * 100 / nearby.MaxHp),
                Damage = ExplosionDamage,
                HitMode = SuPacketHitMode.AttackedInAoe,
                TargetCurrentHp = nearby.Hp,
                TargetMaxHp = nearby.MaxHp,
            }, blasts.Single(su => su.TargetId == nearby.VisualId));
        }

        [TestMethod]
        public async Task AnExplosionSparesTheMonstersAroundIt()
        {
            var wiggler = Spawn(10, 11);
            Spawn(11, 11);
            Explodes(wiggler);

            await SwingsAtTheTargetAsync(wiggler);

            Assert.IsFalse(_hits.Any(h => h.Target.VisualType == VisualType.Monster));
        }

        [TestMethod]
        public async Task AMonsterWithoutAnExplosionOnlyHitsItsTarget()
        {
            var monster = Spawn(10, 11);
            Explodes(Spawn(30, 30));

            await SwingsAtTheTargetAsync(monster);

            Assert.AreEqual(_target.Character.VisualId, _hits.Single().Target.VisualId);
        }

        [TestMethod]
        public async Task AMonsterThatDisappearsAfterHittingDiesByItsOwnHand()
        {
            var wiggler = Spawn(10, 11, disappearsAfterHitting: true);
            Explodes(wiggler);

            await SwingsAtTheTargetAsync(wiggler);

            Assert.IsFalse(wiggler.IsAlive);
            Assert.AreEqual(0, wiggler.Hp);
            PacketAssert.AreEqual(new DiePacket
            {
                VisualType = VisualType.Monster,
                VisualId = wiggler.VisualId,
                TargetVisualType = VisualType.Monster,
                TargetId = wiggler.VisualId,
            }, _map.LastPackets.OfType<DiePacket>().Single());
            _gameEvents.Verify(e => e.RaiseAsync(It.Is<EntityDiedEvent>(d =>
                d.Victim.VisualId == wiggler.VisualId && d.HitSnapshot.Count == 0)), Times.Once);
        }

        [TestMethod]
        public async Task AMonsterThatDoesNotDisappearAfterHittingStaysStanding()
        {
            var monster = Spawn(10, 11);

            await SwingsAtTheTargetAsync(monster);

            Assert.IsTrue(monster.IsAlive);
            Assert.IsFalse(_map.LastPackets.OfType<DiePacket>().Any());
        }
    }
}

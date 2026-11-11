//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.MateExperienceService;
using NosCore.GameObject.Services.MateService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Algorithm.ExperienceShareService;
using NosCore.Algorithm.LevelPenaltyService;
using NosCore.Algorithm.LowLevelBoostService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class VitalsConcurrencyTests
    {
        private TestWorld World;
        private ClientSession _session;
        private MapInstance _map;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = _session.Character.MapInstance;
        }

        private static SkillInfo Skill() => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0, TargetType: 0,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private static HitQueue Queue(int damage)
        {
            var calculator = new Mock<IDamageCalculator>();
            calculator.Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(new DamageResult(damage, SuPacketHitMode.SuccessAttack));
            var stats = new Mock<IBattleStatsProvider>();
            stats.Setup(s => s.GetStats(It.IsAny<IAliveEntity>())).Returns(new CombatStats());
            return new HitQueue(calculator.Object, stats.Object, new Mock<IBuffService>().Object,
                new Mock<IRegenerationService>().Object, new Mock<IVitalityService>().Object,
                new Mock<ILogger<HitQueue>>().Object);
        }

        private Task<HitOutcome> HitAsync(HitQueue queue) =>
            queue.EnqueueAsync(new HitRequest(_session.Character, _session.Character, Skill(), true,
                new TaskCompletionSource<HitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously),
                CancellationToken.None));

        // The map's life tick runs regeneration on the map's executor.
        private Task RegenerationTickAsync(RegenerationService regeneration) =>
            _map.Executor.RunAsync(() => regeneration.TickAsync(_map));

        [TestMethod]
        public async Task RegenerationDoesNotBringBackAPlayerKilledAtTheSameMoment()
        {
            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(e => e.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());
            var regeneration = new RegenerationService(World.SessionRegistry, World.Clock,
                equipment.Object, buffs.Object, NullLogger<RegenerationService>.Instance);
            var lethal = Queue(1_000_000);
            var revived = 0;

            for (var round = 0; round < 500; round++)
            {
                _session.Character.IsAlive = true;
                _session.Character.Hp = 1;
                _session.Character.IsSitting = true;
                World.Clock.Advance(Duration.FromSeconds(2));

                await Task.WhenAll(
                    Task.Run(() => HitAsync(lethal)),
                    Task.Run(() => RegenerationTickAsync(regeneration)));

                if (!_session.Character.IsAlive && _session.Character.Hp > 0)
                {
                    revived++;
                }
            }

            Assert.AreEqual(0, revived, "a dead player kept the HP a regeneration tick gave them");
        }

        [TestMethod]
        public async Task KillsRewardedAtOnceKeepEveryShareOfExperience()
        {
            var experience = new Mock<IExperienceProgressionService>();
            experience.Setup(e => e.AddExperienceAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<long>(),
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()))
                .Returns(async (PlayerComponentBundle player, long levelXp, long _, long _, long _, int _) =>
                {
                    var before = player.LevelXp;
                    await Task.Yield();
                    player.LevelXp = before + levelXp;
                });
            var catalog = new Mock<INpcCombatCatalog>();
            catalog.Setup(c => c.GetDrops(It.IsAny<short>())).Returns(new List<DropDto>());
            var rewards = new RewardService(World.GenerateItemProvider(), World.MapItemProvider.NotNull(),
                catalog.Object, experience.Object, new ExperienceShareService(), new LevelPenaltyService(),
                new LowLevelBoostService(), World.GroupLootService, new MateProgressionService(new MateExperienceService()), NullLogger<RewardService>.Instance);

            const int kills = 50;
            _map.LoadMonsters(
                Enumerable.Range(0, kills + 1).Select(i => new MapMonsterDto { MapMonsterId = i, MapId = 0, MapX = 1, MapY = 1, VNum = 1 }).ToList(),
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, Level = 30, MaxHp = 10, Xp = 1000 } });
            _session.Character.Level = 30;
            _session.Character.LevelXp = 0;
            var player = _session.Character.Handle;
            Task Reward(MonsterComponentBundle monster) =>
                rewards.DistributeAsync(monster, _session.Character, new Dictionary<Entity, int> { [player] = 10 });

            var monsters = _map.Monsters;
            await Reward(monsters[0]);
            var oneKill = _session.Character.LevelXp;
            Assert.IsTrue(oneKill > 0);

            await Task.WhenAll(monsters.Skip(1).Select(monster => Task.Run(() => Reward(monster))));

            Assert.AreEqual(oneKill * (kills + 1), _session.Character.LevelXp);
        }

        [TestMethod]
        public async Task HitsAndOtherHealthChangesOnTheMapDoNotOverlap()
        {
            // Hitting oneself counts as PvP, which halves the blow.
            var hits = Queue(2);
            var lost = 0;

            for (var round = 0; round < 200; round++)
            {
                _session.Character.IsAlive = true;
                _session.Character.Hp = 500;

                await Task.WhenAll(
                    Task.Run(() => HitAsync(hits)),
                    Task.Run(() => _map.Executor.RunAsync(async () =>
                    {
                        var before = _session.Character.Hp;
                        await Task.Yield();
                        _session.Character.Hp = before + 10;
                    })));

                if (_session.Character.Hp != 509)
                {
                    lost++;
                }
            }

            Assert.AreEqual(0, lost);
        }
    }
}

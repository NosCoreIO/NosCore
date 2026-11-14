//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Generated;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Tests.Shared;
using Wolverine;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BattleServiceLifetimeTests
    {
        private TestWorld World;
        [TestMethod]
        public void TheBattleServiceIsASingleton()
        {
            var registration = GeneratedServiceRegistrations.Descriptors
                .Single(d => d.ServiceType == typeof(IBattleService).FullName);

            Assert.AreEqual(ServiceLifetime.Singleton, registration.Lifetime);
        }

        [TestMethod]
        public async Task ACastSkillIsReadyAgainOnceItsCooldownHasRunOut()
        {
            World = await TestWorld.CreateAsync();
            var session = await World.CreateSessionAsync();
            var map = World.GetBaseMap(0);
            session.Character.MapInstance = map;
            map.LoadMonsters(
                new System.Collections.Generic.List<NosCore.Data.Dto.MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = 5, MapY = 4, VNum = 1 } },
                new System.Collections.Generic.List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 } });
            var monster = map.EnumerateMonsters().Single();

            var skill = new SkillInfo(
                SkillVnum: 1, CastId: 7, Cooldown: 6, AttackAnimation: 0, CastEffect: 0, Effect: 0,
                Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0, TargetType: 0,
                Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());
            var skillResolver = new Mock<ISkillResolver>();
            skillResolver.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), 7L)).Returns(skill);
            var targetResolver = new Mock<ITargetResolver>();
            targetResolver.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), skill))
                .Returns(new[] { (IAliveEntity)monster });
            var hitQueue = new Mock<IHitQueue>();
            hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 1, SuPacketHitMode.SuccessAttack, false));
            var clock = new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0));
            var service = new GameObject.Services.BattleService.BattleService(
                skillResolver.Object, targetResolver.Object, hitQueue.Object, new Mock<IMessageBus>().Object,
                World.SessionRegistry, clock, new Mock<ICaptureService>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.ICastDelayService>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.IBuffService>().Object,
                new NosCore.GameObject.Services.BattleService.SkillCastService(clock, new Mock<NosCore.GameObject.Services.EquipmentService.IEquipmentStatsService>().Object, World.Create<BuffService>()),
                new Mock<NosCore.GameObject.Services.BattleService.ISkillComboService>().Object,
                NullLogger<GameObject.Services.BattleService.BattleService>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);

            Assert.IsTrue(await service.Hit(session.Character, monster, new HitArguments { SkillId = 7 }));

            var system = new SkillResetSystem();
            var ready = new System.Collections.Generic.List<(Arch.Core.Entity Entity, long CastId)>();
            system.TakeReady(map.EcsWorld, clock.GetCurrentInstant(), ready);
            Assert.AreEqual(0, ready.Count, "not ready yet");

            clock.Advance(Duration.FromMilliseconds(600));
            system.TakeReady(map.EcsWorld, clock.GetCurrentInstant(), ready);
            CollectionAssert.AreEqual(new[] { (session.Character.Entity, 7L) }, ready);

            ready.Clear();
            system.TakeReady(map.EcsWorld, clock.GetCurrentInstant(), ready);
            Assert.AreEqual(0, ready.Count, "handed out once");
        }
    }
}

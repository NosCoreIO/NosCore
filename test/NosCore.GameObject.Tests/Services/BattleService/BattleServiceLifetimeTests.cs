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
using NosCore.GameObject.Generated;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.PathFinder.Heuristic;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Tests.Shared;
using Wolverine;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // The skill handler schedules a cooldown reset and the map's life loop sends it, so both must
    // reach the same BattleService: with a transient registration the client never got its "sr".
    [TestClass]
    public class BattleServiceLifetimeTests
    {
        [TestMethod]
        public void TheBattleServiceIsASingleton()
        {
            var registration = GeneratedServiceRegistrations.Descriptors
                .Single(d => d.ServiceType == typeof(IBattleService).FullName);

            Assert.AreEqual(ServiceLifetime.Singleton, registration.Lifetime);
        }

        [TestMethod]
        public async Task ACastCharacterIsToldWhenTheSkillIsReadyAgain()
        {
            await TestHelpers.ResetAsync();
            var session = await TestHelpers.Instance.GenerateSessionAsync();
            var map = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            session.Character.MapInstance = map;
            map.LoadMonsters(
                new System.Collections.Generic.List<NosCore.Data.Dto.MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = 5, MapY = 4, VNum = 1 } },
                new System.Collections.Generic.List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 } });
            var monster = map.Monsters.Single();

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
                TestHelpers.Instance.SessionRegistry, clock, new Mock<ICaptureService>().Object,
                new OctileDistanceHeuristic(), NullLogger<GameObject.Services.BattleService.BattleService>.Instance);

            Assert.IsTrue(await service.Hit(session.Character, monster, new HitArguments { SkillId = 7 }));

            await service.TickCooldownResetsAsync(map);
            Assert.IsFalse(session.LastPackets.OfType<SkillResetPacket>().Any(), "not ready yet");

            clock.Advance(Duration.FromMilliseconds(600));
            await service.TickCooldownResetsAsync(map);

            Assert.AreEqual(7, session.LastPackets.OfType<SkillResetPacket>().Single().CastId);
        }
    }
}

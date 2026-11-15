//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class KnockBackTests
    {
        private TestWorld World;
        private ClientSession Attacker;
        private ClientSession Target;
        private CombatStats TargetStats = new();
        private int Roll;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Attacker = await World.CreateSessionAsync();
            Target = await World.CreateSessionAsync();
            Attacker.Character.PositionX = 4;
            Attacker.Character.PositionY = 5;
            Target.Character.PositionX = 5;
            Target.Character.PositionY = 5;

            World.Freeze<Mock<IDamageCalculator>>()
                .Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(new DamageResult(100, SuPacketHitMode.SuccessAttack));
            World.Freeze<Mock<IBattleStatsProvider>>()
                .Setup(s => s.GetStats(It.IsAny<GameObject.Ecs.Interfaces.IAliveEntity>()))
                .Returns(() => TargetStats);
            World.Freeze<Mock<IRandomProvider>>()
                .Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(() => Roll);
        }

        private static BCardDto Card(BCardEffect effect, short first, short second) =>
            new() { Type = effect.Type(), SubType = effect.SubType(), FirstData = first, SecondData = second };

        private Task HitWithAsync(params BCardDto[] bCards) =>
            World.Create<HitQueue>().EnqueueAsync(new HitRequest(
                Origin: Attacker.Character,
                Target: Target.Character,
                Skill: new SkillInfo(1, 1, 0, 0, 0, 0, 0, TargetHitType.SingleTargetHit, 0, 0, 0, 0, 0, 0, bCards),
                IsPrimaryTarget: true,
                Completion: new TaskCompletionSource<HitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously),
                Cancellation: CancellationToken.None));

        [TestMethod]
        public async Task APushBackChancePushesTheTargetAway()
        {
            Roll = 59;

            await HitWithAsync(Card(BCardEffect.JumpBackPushPushBackChance, 60, 2));

            Assert.AreEqual(7, Target.Character.PositionX);
            Assert.AreEqual(5, Target.Character.PositionY);
        }

        [TestMethod]
        public async Task AFailedRollLeavesTheTargetInPlace()
        {
            Roll = 60;

            await HitWithAsync(Card(BCardEffect.JumpBackPushPushBackChance, 60, 2));

            Assert.AreEqual(5, Target.Character.PositionX);
        }

        [TestMethod]
        public async Task ThePvpOnlyPushLeavesAMonsterTargetAlone()
        {
            var monster = World.GetBaseMap(TestWorld.TestMapId)
                .SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 100 }, 5, 5, false);

            await World.Create<HitQueue>().EnqueueAsync(new HitRequest(
                Origin: Attacker.Character,
                Target: monster,
                Skill: new SkillInfo(1, 1, 0, 0, 0, 0, 0, TargetHitType.SingleTargetHit, 0, 0, 0, 0, 0, 0,
                    new[] { Card(BCardEffect.JumpBackPushPushBackChanceNegated, 100, 2) }),
                IsPrimaryTarget: true,
                Completion: new TaskCompletionSource<HitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously),
                Cancellation: CancellationToken.None));

            Assert.AreEqual(5, monster.PositionX);
        }

        [TestMethod]
        public async Task AMonsterIsPushedFromWhereItStandsNotFromItsSpawn()
        {
            Attacker.Character.PositionX = 2;
            var monster = World.GetBaseMap(TestWorld.TestMapId)
                .SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 1000 }, 3, 5, false);
            monster.PositionX = 5;

            await World.Create<HitQueue>().EnqueueAsync(new HitRequest(
                Origin: Attacker.Character,
                Target: monster,
                Skill: new SkillInfo(1, 1, 0, 0, 0, 0, 0, TargetHitType.SingleTargetHit, 0, 0, 0, 0, 0, 0,
                    new[] { Card(BCardEffect.SpecialActionsPushBack, 1, 0) }),
                IsPrimaryTarget: true,
                Completion: new TaskCompletionSource<HitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously),
                Cancellation: CancellationToken.None));

            Assert.AreEqual(6, monster.PositionX);
        }

        [TestMethod]
        public async Task ThePvpOnlyPushMovesAPlayer()
        {
            await HitWithAsync(Card(BCardEffect.JumpBackPushPushBackChanceNegated, 100, 1));

            Assert.AreEqual(6, Target.Character.PositionX);
        }

        [TestMethod]
        public async Task AJumpBackMovesTheAttackerAwayFromTheTarget()
        {
            await HitWithAsync(Card(BCardEffect.JumpBackPushJumpBackChance, 100, 2));

            Assert.AreEqual(2, Attacker.Character.PositionX);
            Assert.AreEqual(5, Target.Character.PositionX);
        }

        [TestMethod]
        public async Task ResistanceToForcedMovementKeepsTheTargetInPlace()
        {
            TargetStats = new CombatStats { ForcedMovementResistance = 100 };

            await HitWithAsync(Card(BCardEffect.JumpBackPushPushBackChance, 100, 2));

            Assert.AreEqual(5, Target.Character.PositionX);
        }

        [TestMethod]
        public async Task ResistanceDoesNotStopTheAttackersOwnJump()
        {
            TargetStats = new CombatStats { ForcedMovementResistance = 100 };

            await HitWithAsync(Card(BCardEffect.JumpBackPushJumpBackChance, 100, 1));

            Assert.AreEqual(3, Attacker.Character.PositionX);
        }
    }
}

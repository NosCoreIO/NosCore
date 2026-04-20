//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BattleServiceTests
    {
        private IBattleService Service = null!;
        private Mock<IAliveEntity> Origin = null!;
        private Mock<IAliveEntity> Target = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Service = new GameObject.Services.BattleService.BattleService();
            Origin = BuildAliveEntityMock(hp: 1000, noAttack: false);
            Target = BuildAliveEntityMock(hp: 1000, noAttack: false);
        }

        [TestMethod]
        public async Task HitOnTargetWithNoAttackTrueLeavesTargetHpUnchanged()
        {
            // NPCs and other non-combat entities expose NoAttack=true; treat as untargetable.
            // Without the gate, an upgrade NPC like Smith Malcolm could be killed via UseSkill
            // packets, breaking the n_run flow.
            await new Spec("Hit on a NoAttack target cancels and leaves target HP unchanged")
                .Given(TargetHasNoAttackTrue)
                .WhenAsync(HitIsExecuted)
                .Then(TargetHpShouldBeUnchanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HitWhenOriginHasNoAttackTrueLeavesTargetHpUnchanged()
        {
            // Pre-existing gate, kept covered.
            await new Spec("Hit cancels when origin has NoAttack=true")
                .Given(OriginHasNoAttackTrue)
                .WhenAsync(HitIsExecuted)
                .Then(TargetHpShouldBeUnchanged)
                .ExecuteAsync();
        }

        // --- Givens ---

        private void TargetHasNoAttackTrue() => Target.SetupGet(t => t.NoAttack).Returns(true);

        private void OriginHasNoAttackTrue() => Origin.SetupGet(t => t.NoAttack).Returns(true);

        // --- Whens ---

        private Task HitIsExecuted() => Service.Hit(Origin.Object, Target.Object, new HitArguments());

        // --- Thens ---

        private void TargetHpShouldBeUnchanged() =>
            Target.VerifySet(t => t.Hp = It.IsAny<int>(), Times.Never);

        // --- Helpers ---

        private static Mock<IAliveEntity> BuildAliveEntityMock(int hp, bool noAttack)
        {
            var mock = new Mock<IAliveEntity>();
            mock.SetupGet(t => t.Hp).Returns(hp);
            mock.SetupGet(t => t.MaxHp).Returns(hp);
            mock.SetupGet(t => t.NoAttack).Returns(noAttack);
            mock.SetupGet(t => t.Handle).Returns(Entity.Null);
            mock.SetupGet(t => t.HitSemaphore).Returns(new SemaphoreSlim(1, 1));
            mock.SetupGet(t => t.HitList).Returns(new ConcurrentDictionary<Entity, int>());
            return mock;
        }
    }
}

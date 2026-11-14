//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Algorithm.RecoveryRateService;
using Arch.Core;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class RegenerationServiceTests
    {
        private TestWorld World;
        private ClientSession Session;
        private RegenerationService Regeneration;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject<IRecoveryRateService>(new RecoveryRateService());
            World.Freeze<Mock<IEquipmentStatsService>>()
                .Setup(e => e.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            World.Freeze<Mock<IBuffService>>()
                .Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());
            Regeneration = World.Create<RegenerationService>();
            Session = await World.CreateSessionAsync();
            Session.Character.Level = 50;
            Session.Character.Hp = 1;
            Session.Character.IsSitting = false;
        }

        private Task TickAsync() =>
            Session.Character.MapInstance.Executor.RunAsync(async () =>
            {
                var due = new List<Entity>();
                new RegenerationSystem().FindDue(Session.Character.MapInstance.EcsWorld, World.Clock.GetCurrentInstant(), due);
                foreach (var entity in due)
                {
                    await Regeneration.RegenerateAsync(new PlayerComponentBundle(entity, Session.Character.MapInstance.EcsWorld));
                }
            });

        private async Task TickAfterAsync(int seconds)
        {
            World.Clock.Advance(Duration.FromSeconds(seconds));
            await TickAsync();
        }

        [TestMethod]
        public async Task TheFirstTickOnlyStartsTheInterval()
        {
            await TickAsync();

            Assert.AreEqual(1, Session.Character.Hp);
        }

        [TestMethod]
        public async Task AStandingPlayerRegeneratesOnceTheIntervalHasPassed()
        {
            await TickAsync();
            await TickAfterAsync(2);

            Assert.IsTrue(Session.Character.Hp > 1);
        }

        [TestMethod]
        public async Task AHitHoldsStandingRegenerationForFourSeconds()
        {
            await TickAsync();
            Regeneration.NotifyDamaged(Session.Character);
            await TickAfterAsync(3);

            Assert.AreEqual(1, Session.Character.Hp);

            await TickAfterAsync(2);

            Assert.IsTrue(Session.Character.Hp > 1);
        }
    }
}

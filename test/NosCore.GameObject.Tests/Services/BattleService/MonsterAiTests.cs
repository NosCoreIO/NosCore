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
using NodaTime.Testing;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.PathfindingService;
using NosCore.PathFinder.Interfaces;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // The AI pulls in a MonsterComponentBundle which is a generated ECS struct tied to
    // a live MapWorld. Constructing one end-to-end for a unit test is expensive; these
    // tests focus on the outer wiring. End-to-end AI behaviour is covered by the
    // in-process map fixture tests that exercise StartLifeAsync.
    [TestClass]
    public class MonsterAiTests
    {
        [TestMethod]
        public void ConstructsWithAllDependencies()
        {
            var ai = new MonsterAi(
                new Mock<IBattleService>().Object,
                new Mock<IAggroService>().Object,
                new Mock<IPathfindingService>().Object,
                new Mock<ISessionRegistry>().Object,
                new Mock<IHeuristic>().Object,
                new Mock<INpcCombatCatalog>().Object,
                new Mock<IRandomProvider>().Object,
                new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0)),
                new Dictionary<short, SkillDto>(),
                new Mock<ILogger<MonsterAi>>().Object);

            Assert.IsNotNull(ai);
            Assert.IsInstanceOfType(ai, typeof(IMonsterAi));
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class PlayerRevivalHandlerTests
    {
        [TestMethod]
        public async Task NonPlayerVictimIsIgnored()
        {
            // Monster deaths go to MonsterRespawnHandler — the revival handler must
            // opt out silently so both can coexist behind the same EntityDiedEvent.
            var handler = new PlayerRevivalHandler(new Mock<ILogger>().Object);

            var victim = new Mock<IAliveEntity>();
            victim.SetupGet(v => v.VisualType).Returns(VisualType.Monster);

            // Should return without throwing — no side effects to assert on.
            await handler.Handle(new EntityDiedEvent(victim.Object, null));
        }
    }
}

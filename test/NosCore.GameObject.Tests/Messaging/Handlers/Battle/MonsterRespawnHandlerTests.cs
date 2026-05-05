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
using NosCore.GameObject.Services.BattleService;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class MonsterRespawnHandlerTests
    {
        [TestMethod]
        public async Task PlayerDeathIsIgnored()
        {
            // Player corpses go through the revive flow, not the respawn handler.
            // Passing a non-monster victim should be a no-op (no aggro clear either,
            // since the player doesn't own monster aggro state).
            var aggro = new Mock<IAggroService>();
            var handler = new MonsterRespawnHandler(aggro.Object, NodaTime.SystemClock.Instance);

            var playerVictim = new Mock<IAliveEntity>().Object;
            var killer = new Mock<IAliveEntity>().Object;

            await handler.Handle(new EntityDiedEvent(playerVictim, killer));

            aggro.Verify(a => a.Clear(It.IsAny<IAliveEntity>()), Times.Never);
        }
    }
}

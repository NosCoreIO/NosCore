//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class MonsterAggroHandlerTests
    {
        [TestMethod]
        public async Task DamageOnMonsterBumpsAggro()
        {
            var aggro = new Mock<IAggroService>();
            var handler = new MonsterAggroHandler(aggro.Object);

            var monster = new Mock<INonPlayableEntity>();
            var player = new Mock<IAliveEntity>().Object;

            await handler.Handle(new EntityDamagedEvent(player, monster.Object, 42, false));

            aggro.Verify(a => a.AddThreat(monster.Object, player, 42), Times.Once);
        }

        [TestMethod]
        public async Task DamageOnPlayerDoesNothing()
        {
            var aggro = new Mock<IAggroService>();
            var handler = new MonsterAggroHandler(aggro.Object);

            var attacker = new Mock<IAliveEntity>().Object;
            var victim = new Mock<IAliveEntity>().Object;

            await handler.Handle(new EntityDamagedEvent(attacker, victim, 10, false));

            aggro.Verify(a => a.AddThreat(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<int>()), Times.Never);
        }
    }
}

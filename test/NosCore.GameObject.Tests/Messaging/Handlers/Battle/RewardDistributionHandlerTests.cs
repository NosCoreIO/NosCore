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

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class RewardDistributionHandlerTests
    {
        [TestMethod]
        public async Task HandleForwardsToRewardService()
        {
            var rewards = new Mock<IRewardService>();
            var handler = new RewardDistributionHandler(rewards.Object);

            var victim = new Mock<IAliveEntity>().Object;
            var killer = new Mock<IAliveEntity>().Object;

            await handler.Handle(new EntityDiedEvent(victim, killer));

            rewards.Verify(r => r.DistributeAsync(victim, killer), Times.Once);
        }
    }
}

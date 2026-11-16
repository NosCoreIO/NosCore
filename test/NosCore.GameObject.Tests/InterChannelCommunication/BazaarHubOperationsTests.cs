//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class BazaarHubOperationsTests
    {
        [TestMethod]
        public async Task ARequestThatFailsTwiceIsAnsweredOnTheThirdAttempt()
        {
            var attempts = 0;

            var answer = await BazaarHubOperations.RequestAsync(() =>
                ++attempts < 3 ? throw new TimeoutException() : Task.FromResult(true));

            Assert.IsTrue(answer);
            Assert.AreEqual(3, attempts);
        }

        [TestMethod]
        public async Task ARequestThatNeverGetsAnAnswerReportsTheMasterAsUnreachable()
        {
            var cause = new TimeoutException();

            var unreachable = await Assert.ThrowsExactlyAsync<MasterUnreachableException>(() =>
                BazaarHubOperations.RequestAsync<bool>(() => throw cause));

            Assert.AreSame(cause, unreachable.InnerException);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    // The two percentages in `st` are the health and mana bars the client draws.
    //
    // Every other place that computes them guards the maximum first; GenerateStatInfo was the one
    // that did not. It never threw and never would: the division is on floats, so a maximum of
    // zero yields NaN and the cast puts a meaningless number in the packet. Nothing in a log, and
    // a bar that reads as whatever that number happened to be.
    [TestClass]
    public class StatInfoPercentageTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        [TestMethod]
        public void WithARealMaximumThePercentagesAreTheOrdinaryOnes()
        {
            _session.Character.MaxHp = 1000;
            _session.Character.Hp = 250;
            _session.Character.MaxMp = 400;
            _session.Character.Mp = 400;

            var packet = _session.Character.GenerateStatInfo();

            Assert.AreEqual(25, packet.HpPercentage);
            Assert.AreEqual(100, packet.MpPercentage);
        }

        [TestMethod]
        public void AZeroMaximumDoesNotPutNonsenseInTheBar()
        {
            _session.Character.MaxHp = 0;
            _session.Character.Hp = 0;
            _session.Character.MaxMp = 0;
            _session.Character.Mp = 0;

            var packet = _session.Character.GenerateStatInfo();

            // The value matters less than the fact that it is a value: unguarded, the cast of a
            // NaN gives whatever the platform gives, and the assertion below would be a coin toss.
            Assert.AreEqual(100, packet.HpPercentage);
            Assert.AreEqual(100, packet.MpPercentage);
        }

        // The same packet built by the same call for the same character has to agree with the
        // guarded computation the rest of the file uses.
        [TestMethod]
        public void ItAgreesWithTheGuardedComputationElsewhere()
        {
            _session.Character.MaxHp = 777;
            _session.Character.Hp = 111;

            var packet = _session.Character.GenerateStatInfo();

            Assert.AreEqual((int)(111 / 777f * 100), packet.HpPercentage);
        }
    }
}

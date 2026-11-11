//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ShellOptionService
{
    [TestClass]
    public class ShellOptionServiceTests
    {
        private ClientSession _session;
        private ShellTestBench _bench;
        private WearableInstance _shell;
        private int _rolled;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _bench = new ShellTestBench();
        }

        [TestMethod]
        public async Task AFullShellRollsOneOptionPerSlot()
        {
            await new Spec("A rarity 5 full weapon shell rolls a C, a B and an A option, each a different type")
                .Given(AWeaponShell_OfRarity_AndLevel_, ShellType.Full, (short)5, (byte)60)
                .And(EveryRollIs_, 0d)
                .When(RollingTheShell)
                .Then(Option_LevelsShouldBe_, "1 2 3")
                .And(TheFirstOptionShouldBeEnhancedDamageOf_, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OptionalSlotsCanBeSkipped()
        {
            await new Spec("A perfect shell drops its half-chance slots when every roll misses")
                .Given(AWeaponShell_OfRarity_AndLevel_, ShellType.Perfect, (short)1, (byte)60)
                .And(EveryRollIs_, 0.99d)
                .When(RollingTheShell)
                .Then(Option_LevelsShouldBe_, "1")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AShellWithoutAKindRollsNothing()
        {
            await new Spec("A shell rarity no table covers rolls nothing")
                .Given(AWeaponShell_OfRarity_AndLevel_, ShellType.Full, (short)8, (byte)60)
                .And(EveryRollIs_, 0d)
                .When(RollingTheShell)
                .Then(NoOptionShouldBeRolled)
                .ExecuteAsync();
        }

        private void AWeaponShell_OfRarity_AndLevel_(ShellType type, short rare, byte level) =>
            _shell = (WearableInstance)ShellTestBench.Shell(_session, 0, 0, type, rare, level).ItemInstance;

        private void EveryRollIs_(double roll) => _bench.Random.Setup(r => r.NextDouble()).Returns(roll);

        private void RollingTheShell() => _rolled = _bench.Service.Roll(_shell).Count;

        private void Option_LevelsShouldBe_(string levels) =>
            Assert.AreEqual(levels, string.Join(" ", _bench.Service.Roll(_shell).Select(o => o.Level)));

        private void TheFirstOptionShouldBeEnhancedDamageOf_(int value)
        {
            var first = _bench.Service.Roll(_shell)[0];
            Assert.AreEqual((byte)WeaponShellOptionType.EnhancedDamage, first.Type);
            Assert.AreEqual(value, first.Value);
        }

        private void NoOptionShouldBeRolled() => Assert.AreEqual(0, _rolled);
    }
}

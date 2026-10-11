//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.DigestionService
{
    [TestClass]
    public class DigestionServiceTests
    {
        private ClientSession _session = null!;
        private GameObject.Services.DigestionService.DigestionService _service = null!;
        private DigestionState _digestion = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MaxHp = 1000;
            _session.Character.MaxMp = 1000;
            _session.Character.IsSitting = true;
            _digestion = _session.Character.Digestion;
            _digestion.LastTick = TestHelpers.Instance.Clock.GetCurrentInstant();
            _service = new GameObject.Services.DigestionService.DigestionService(
                TestHelpers.Instance.SessionRegistry, TestHelpers.Instance.Clock,
                NullLogger<GameObject.Services.DigestionService.DigestionService>.Instance);
        }

        [TestMethod]
        public async Task NothingHealsBeforeTwoSeconds()
        {
            await new Spec("A tick due in two seconds does not land after one")
                .Given(CharacterHas_Hp_Mp, 100, 100)
                .And(FoodOwes_Hp_MpPerTick, 140, 0)
                .And(_SecondsPass, 1)
                .WhenAsync(TheMapTicks)
                .Then(HpShouldBe_, 100)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATickHealsAndShowsTheGreenNumber()
        {
            await new Spec("After two seconds one fifth heals, rc shows it to the map and stat refreshes the bars")
                .Given(CharacterHas_Hp_Mp, 100, 100)
                .And(FoodOwes_Hp_MpPerTick, 140, 0)
                .And(_SecondsPass, 2)
                .WhenAsync(TheMapTicks)
                .Then(HpShouldBe_, 240)
                .And(TheMapShouldSeeAHealOf_, 140)
                .And(TheBarsShouldBeRefreshed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALateTickDoesNotPushBackTheNextOne()
        {
            await new Spec("The map loop picks a tick up 400 ms late; the next is still due two seconds after the first was")
                .Given(CharacterHas_Hp_Mp, 100, 100)
                .And(FoodOwes_Hp_MpPerTick, 140, 0)
                .And(_MillisecondsPass, 2400)
                .AndAsync(TheMapTicks)
                .And(_MillisecondsPass, 1600)
                .WhenAsync(TheMapTicks)
                .Then(HpShouldBe_, 380)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TicksDoNotBunchUpAfterAStall()
        {
            await new Spec("After a long stall one tick lands, and the next waits the full two seconds")
                .Given(CharacterHas_Hp_Mp, 100, 100)
                .And(FoodOwes_Hp_MpPerTick, 140, 0)
                .And(_MillisecondsPass, 10000)
                .AndAsync(TheMapTicks)
                .And(_MillisecondsPass, 400)
                .WhenAsync(TheMapTicks)
                .Then(HpShouldBe_, 240)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFoodTickNeverHealsMoreThanSixHundred()
        {
            await new Spec("However much food is digesting, one tick heals at most 600")
                .Given(CharacterHasMaximum_Hp, 5000)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .And(FoodOwes_Hp_MpPerTick, 700, 0)
                .And(_SecondsPass, 2)
                .WhenAsync(TheMapTicks)
                .Then(HpShouldBe_, 700)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABigSnackTickIsNotCapped()
        {
            await new Spec("A snack tick heals its full amount, even above 600")
                .Given(CharacterHasMaximum_Hp, 5000)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .And(SnackOwes_Hp_MpPerTick, 1000, 0)
                .And(_SecondsPass, 2)
                .WhenAsync(TheMapTicks)
                .Then(HpShouldBe_, 1100)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HealingStopsAtTheMaximum()
        {
            await new Spec("A tick never lifts HP past the maximum, and rc shows what actually healed")
                .Given(CharacterHas_Hp_Mp, 950, 100)
                .And(FoodOwes_Hp_MpPerTick, 140, 0)
                .And(_SecondsPass, 2)
                .WhenAsync(TheMapTicks)
                .Then(HpShouldBe_, 1000)
                .And(TheMapShouldSeeAHealOf_, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ManaOnlyHealingSendsNoGreenNumber()
        {
            await new Spec("rc carries HP, so a mana-only tick only refreshes the bars")
                .Given(CharacterHas_Hp_Mp, 100, 100)
                .And(FoodOwes_Hp_MpPerTick, 0, 500)
                .And(_SecondsPass, 2)
                .WhenAsync(TheMapTicks)
                .Then(MpShouldBe_, 600)
                .And(NoHealShouldBeShown)
                .And(TheBarsShouldBeRefreshed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StandingUpThrowsAwayTheFoodButNotTheSnack()
        {
            await new Spec("Food only heals while resting; snacks keep going")
                .Given(CharacterHas_Hp_Mp, 100, 100)
                .And(FoodOwes_Hp_MpPerTick, 140, 0)
                .And(SnackOwes_Hp_MpPerTick, 50, 0)
                .And(TheCharacterStandsUp)
                .And(_SecondsPass, 2)
                .WhenAsync(TheMapTicks)
                .Then(HpShouldBe_, 150)
                .And(TheFoodScheduleShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeathEmptiesBothSchedules()
        {
            await new Spec("Nothing keeps digesting on a dead character")
                .Given(FoodOwes_Hp_MpPerTick, 140, 0)
                .And(SnackOwes_Hp_MpPerTick, 50, 0)
                .And(TheCharacterDies)
                .And(_SecondsPass, 2)
                .WhenAsync(TheMapTicks)
                .Then(TheFoodScheduleShouldBeEmpty)
                .And(TheSnackScheduleShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AtFullHealthTheTickPassesSilently()
        {
            await new Spec("At full HP and MP the slot is used up but nothing is sent")
                .Given(CharacterHas_Hp_Mp, 1000, 1000)
                .And(FoodOwes_Hp_MpPerTick, 140, 140)
                .And(_SecondsPass, 2)
                .WhenAsync(TheMapTicks)
                .Then(NoHealShouldBeShown)
                .And(NoBarRefreshShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterHas_Hp_Mp(int hp, int mp)
        {
            _session.Character.Hp = hp;
            _session.Character.Mp = mp;
        }

        private void CharacterHasMaximum_Hp(int maxHp) => _session.Character.MaxHp = maxHp;

        private void FoodOwes_Hp_MpPerTick(int hp, int mp) => _digestion.Food.Add(hp, mp);

        private void SnackOwes_Hp_MpPerTick(int hp, int mp) => _digestion.Snack.Add(hp, mp);

        private void _SecondsPass(int seconds) => TestHelpers.Instance.Clock.Advance(Duration.FromSeconds(seconds));

        private void _MillisecondsPass(int milliseconds) =>
            TestHelpers.Instance.Clock.Advance(Duration.FromMilliseconds(milliseconds));

        private void TheCharacterStandsUp() => _session.Character.IsSitting = false;

        private void TheCharacterDies() => _session.Character.IsAlive = false;

        private async Task TheMapTicks()
        {
            _session.LastPackets.Clear();
            _session.Character.MapInstance.LastPackets.Clear();
            await _service.TickPlayerAsync(_session.Character);
        }

        private void HpShouldBe_(int expected) => Assert.AreEqual(expected, _session.Character.Hp);

        private void MpShouldBe_(int expected) => Assert.AreEqual(expected, _session.Character.Mp);

        private void TheMapShouldSeeAHealOf_(int amount)
        {
            var rc = _session.Character.MapInstance.LastPackets.OfType<RcPacket>().SingleOrDefault();
            Assert.IsNotNull(rc);
            Assert.AreEqual(_session.Character.VisualId, rc.VisualId);
            Assert.AreEqual(amount, rc.HealAmount);
        }

        private void NoHealShouldBeShown() =>
            Assert.IsFalse(_session.Character.MapInstance.LastPackets.OfType<RcPacket>().Any());

        private void TheBarsShouldBeRefreshed() => Assert.IsTrue(_session.LastPackets.OfType<StatPacket>().Any());

        private void NoBarRefreshShouldBeSent() => Assert.IsFalse(_session.LastPackets.OfType<StatPacket>().Any());

        private void TheFoodScheduleShouldBeEmpty() => Assert.IsTrue(_digestion.Food.IsEmpty);

        private void TheSnackScheduleShouldBeEmpty() => Assert.IsTrue(_digestion.Snack.IsEmpty);
    }
}

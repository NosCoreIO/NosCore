//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ComplimentService;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ComplimentService
{
    [TestClass]
    public class ComplimentServiceTests
    {
        private TestWorld World;
        private GameObject.Services.ComplimentService.ComplimentService Service;
        private ComplimentResult Result;
        private ClientSession Session;
        private ClientSession Target;
        private short InitialCompliment;
        private readonly List<MapTypeMapDto> MapTypeMaps = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            Target = await World.GenerateSessionAsync();
            Session.Character.Level = 31;
            InitialCompliment = Target.Character.Compliment;
            Service = new GameObject.Services.ComplimentService.ComplimentService(World.Clock,
                World.SessionRegistry, MapTypeMaps);
        }

        [TestMethod]
        public async Task ComplimentShouldIncreaseTargetCompliment()
        {
            await new Spec("Compliment should increase target compliment")
                .Given(OneHourHasPassedSinceConnection)
                .When(ComplimentingTarget)
                .Then(TargetComplimentShouldBeIncreasedBy_, 1)
                .And(OutcomeShouldBe_, ComplimentOutcome.Complimented)
                .And(TheResultShouldNameTheTarget)
                .And(LastComplimentShouldBeRecorded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentAtLevelThirtyShouldBeRefused()
        {
            await new Spec("Compliment at level thirty should be refused")
                .Given(SenderIsLevel_, (byte)30)
                .And(OneHourHasPassedSinceConnection)
                .When(ComplimentingTarget)
                .Then(TargetComplimentShouldBeIncreasedBy_, 0)
                .And(OutcomeShouldBe_, ComplimentOutcome.LevelTooLow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentWithinAnHourOfConnectionShouldBeRefused()
        {
            await new Spec("Compliment within an hour of connection should be refused")
                .Given(_MinutesHavePassedSinceConnection, 20)
                .When(ComplimentingTarget)
                .Then(TargetComplimentShouldBeIncreasedBy_, 0)
                .And(OutcomeShouldBe_, ComplimentOutcome.TooSoonAfterConnection)
                .And(MinutesToWaitShouldBe_, 40L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SecondComplimentOnSameDayShouldBeRefused()
        {
            await new Spec("Second compliment on same day should be refused")
                .Given(OneHourHasPassedSinceConnection)
                .And(ComplimentingTarget)
                .When(ComplimentingTarget)
                .Then(TargetComplimentShouldBeIncreasedBy_, 1)
                .And(OutcomeShouldBe_, ComplimentOutcome.AlreadyComplimentedToday)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentOnNextDayShouldSucceed()
        {
            await new Spec("Compliment on next day should succeed")
                .Given(OneHourHasPassedSinceConnection)
                .And(ComplimentingTarget)
                .And(ADayHasPassed)
                .When(ComplimentingTarget)
                .Then(TargetComplimentShouldBeIncreasedBy_, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentingSelfShouldDoNothing()
        {
            await new Spec("Complimenting self finds no target")
                .Given(OneHourHasPassedSinceConnection)
                .When(ComplimentingSelf)
                .Then(SenderComplimentShouldBeUnchanged)
                .And(OutcomeShouldBe_, ComplimentOutcome.NoTarget)
                .And(LastComplimentShouldNotBeRecorded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentInGlacernonShouldBeRefused()
        {
            await new Spec("Compliment in Glacernon should be refused")
                .Given(SenderIsOnAMapOfType_, MapTypeType.Act4)
                .And(OneHourHasPassedSinceConnection)
                .When(ComplimentingTarget)
                .Then(TargetComplimentShouldBeIncreasedBy_, 0)
                .And(OutcomeShouldBe_, ComplimentOutcome.InGlacernon)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentingAbsentCharacterFindsNoTarget()
        {
            await new Spec("Complimenting an absent character finds no target")
                .Given(SenderIsLevel_, (byte)29)
                .When(ComplimentingAbsentCharacter)
                .Then(OutcomeShouldBe_, ComplimentOutcome.NoTarget)
                .ExecuteAsync();
        }

        private void SenderIsOnAMapOfType_(MapTypeType type) =>
            MapTypeMaps.Add(new MapTypeMapDto { MapId = Session.Character.MapInstance.Map.MapId, MapTypeId = (short)type });

        private void SenderIsLevel_(byte level) => Session.Character.Level = level;

        private void OneHourHasPassedSinceConnection() => World.Clock.AdvanceMinutes(60);

        private void _MinutesHavePassedSinceConnection(int minutes) => World.Clock.AdvanceMinutes(minutes);

        private void ADayHasPassed() => World.Clock.AdvanceHours(24);

        private void ComplimentingTarget()
        {
            Result = Service.Compliment(Session.Character, Target.Character.CharacterId);
        }

        private void ComplimentingSelf()
        {
            Result = Service.Compliment(Session.Character, Session.Character.CharacterId);
        }

        private void ComplimentingAbsentCharacter()
        {
            Result = Service.Compliment(Session.Character, long.MaxValue);
        }

        private void TargetComplimentShouldBeIncreasedBy_(int amount) =>
            Assert.AreEqual(InitialCompliment + amount, Target.Character.Compliment);

        private void SenderComplimentShouldBeUnchanged() =>
            Assert.AreEqual(InitialCompliment, Session.Character.Compliment);

        private void OutcomeShouldBe_(ComplimentOutcome outcome) => Assert.AreEqual(outcome, Result.Outcome);

        private void TheResultShouldNameTheTarget() =>
            Assert.AreEqual(Target.Character.CharacterId, Result.Target?.CharacterId);

        private void MinutesToWaitShouldBe_(long minutes) => Assert.AreEqual(minutes, Result.MinutesToWait);

        private void LastComplimentShouldBeRecorded() =>
            Assert.AreEqual(World.Clock.GetCurrentInstant(), Session.Character.Account.LastCompliment);

        private void LastComplimentShouldNotBeRecorded() =>
            Assert.IsNull(Session.Character.Account.LastCompliment);
    }
}

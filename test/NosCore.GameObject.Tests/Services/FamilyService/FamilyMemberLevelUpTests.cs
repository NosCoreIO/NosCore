//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Tests.Shared;
using SpecLight;
using CharacterExperience = NosCore.Algorithm.ExperienceService.ExperienceService;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyMemberLevelUpTests
    {
        private const long FamilyId = 42;

        private readonly CharacterExperience _curve = new();
        private TestWorld World;
        private ClientSession _session;
        private Mock<IFamilyContributionService> _contribution;
        private ExperienceProgressionService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _contribution = World.Freeze<Mock<IFamilyContributionService>>();
            _service = World.Create<ExperienceProgressionService>();
        }

        [TestMethod]
        public async Task AMemberWhoLevelsUpTellsTheFamily()
        {
            await new Spec("A family member who gains a level reports the levels passed to the family")
                .Given(TheCharacterIsInAFamily)
                .And(TheCharacterIsOneExperienceShortOfLeavingLevel_, 29)
                .WhenAsync(TheCharacterEarns_Experience, 1L)
                .Then(TheFamilyShouldBeToldTheMemberWentFromLevel_To_, 29, 30)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SeveralLevelsAtOnceAreReportedTogether()
        {
            await new Spec("Experience that carries a member over several levels reports the first and the last")
                .Given(TheCharacterIsInAFamily)
                .And(TheCharacterIsOneExperienceShortOfLeavingLevel_, 29)
                .WhenAsync(TheCharacterEarns_Experience, 1 + _curve.GetExperience(30))
                .Then(TheFamilyShouldBeToldTheMemberWentFromLevel_To_, 29, 31)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMemberWhoStaysOnTheSameLevelTellsNothing()
        {
            await new Spec("Experience that does not fill the bar reports nothing to the family")
                .Given(TheCharacterIsInAFamily)
                .And(TheCharacterIsOneExperienceShortOfLeavingLevel_, 29)
                .WhenAsync(TheCharacterEarns_Experience, 0L)
                .Then(NoFamilyShouldBeTold)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SomeoneWithoutAFamilyTellsNothing()
        {
            await new Spec("A character without a family levels up without reporting to any family")
                .Given(TheCharacterIsOneExperienceShortOfLeavingLevel_, 29)
                .WhenAsync(TheCharacterEarns_Experience, 1L)
                .Then(NoFamilyShouldBeTold)
                .ExecuteAsync();
        }

        private void TheCharacterIsInAFamily() =>
            _session.Character.Family = new Family { FamilyId = FamilyId, Name = "Testers", MaxSize = 50 };

        private void TheCharacterIsOneExperienceShortOfLeavingLevel_(int level)
        {
            _session.Character.Level = (byte)level;
            _session.Character.LevelXp = _curve.GetExperience((byte)level) - 1;
        }

        private Task TheCharacterEarns_Experience(long experience) =>
            _service.AddExperienceAsync(_session.Character, experience, 0, 0, 0, 0);

        private void TheFamilyShouldBeToldTheMemberWentFromLevel_To_(int previous, int level)
        {
            var characterId = _session.Character.CharacterId;
            var name = _session.Character.Name;
            _contribution.Verify(s => s.CharacterLevelledUpAsync(FamilyId, characterId, name, (byte)previous, (byte)level), Times.Once);
        }

        private void NoFamilyShouldBeTold() =>
            _contribution.Verify(s => s.CharacterLevelledUpAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<byte>(), It.IsAny<byte>()), Times.Never);
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Encryption;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ReputationRankingService
{
    [TestClass]
    public class ReputationRankingServiceTests
    {
        private const long SenderId = 1000;
        private const long SenderReputation = 5000;

        private GameObject.Services.ReputationRankingService.ReputationRankingService Service;
        private bool Result;
        private long NextId = 2000;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Service = new GameObject.Services.ReputationRankingService.ReputationRankingService(
                TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao,
                TestHelpers.Instance.WorldConfiguration, TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task PlayerWithTwoRivalsAheadIsTopThree()
        {
            await new Spec("Player with two rivals ahead is top three")
                .GivenAsync(TwoPlayersHaveMoreReputation)
                .When(CheckingTheSender)
                .Then(SenderShouldBeTopThree)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayerWithThreeRivalsAheadIsNotTopThree()
        {
            await new Spec("Player with three rivals ahead is not top three")
                .GivenAsync(ThreePlayersHaveMoreReputation)
                .When(CheckingTheSender)
                .Then(SenderShouldNotBeTopThree)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StaffAheadDoNotTakePlaces()
        {
            await new Spec("Staff ahead do not take places")
                .GivenAsync(TwoPlayersHaveMoreReputation)
                .AndAsync(AGameMasterHasMoreReputation)
                .When(CheckingTheSender)
                .Then(SenderShouldBeTopThree)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletedCharactersDoNotTakePlaces()
        {
            await new Spec("Deleted characters do not take places")
                .GivenAsync(TwoPlayersHaveMoreReputation)
                .AndAsync(ADeletedCharacterHasMoreReputation)
                .When(CheckingTheSender)
                .Then(SenderShouldBeTopThree)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StaffAreNeverRanked()
        {
            await new Spec("Staff are never ranked")
                .When(CheckingAGameMaster)
                .Then(SenderShouldNotBeTopThree)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RepeatedChecksReuseTheRanking()
        {
            await new Spec("Repeated checks reuse the ranking")
                .GivenAsync(TwoPlayersHaveMoreReputation)
                .And(CheckingTheSender)
                .AndAsync(AThirdPlayerOvertakesTheSender)
                .When(CheckingTheSender)
                .Then(SenderShouldBeTopThree)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheRankingIsRefreshedOnceItsIntervalHasPassed()
        {
            await new Spec("The ranking is refreshed once its interval has passed")
                .GivenAsync(TwoPlayersHaveMoreReputation)
                .And(CheckingTheSender)
                .AndAsync(AThirdPlayerOvertakesTheSender)
                .And(TheRefreshIntervalPasses)
                .When(CheckingTheSender)
                .Then(SenderShouldNotBeTopThree)
                .ExecuteAsync();
        }

        private Task AThirdPlayerOvertakesTheSender() =>
            AddCharacterAsync(AuthorityType.User, SenderReputation + 3, CharacterState.Active);

        private void TheRefreshIntervalPasses() =>
            TestHelpers.Instance.Clock.Advance(GameObject.Services.ReputationRankingService.ReputationRankingService.RefreshInterval);

        private async Task TwoPlayersHaveMoreReputation()
        {
            await AddCharacterAsync(AuthorityType.User, SenderReputation + 1, CharacterState.Active);
            await AddCharacterAsync(AuthorityType.User, SenderReputation + 2, CharacterState.Active);
        }

        private async Task ThreePlayersHaveMoreReputation()
        {
            await TwoPlayersHaveMoreReputation();
            await AddCharacterAsync(AuthorityType.User, SenderReputation + 3, CharacterState.Active);
        }

        private Task AGameMasterHasMoreReputation() =>
            AddCharacterAsync(AuthorityType.GameMaster, SenderReputation * 10, CharacterState.Active);

        private Task ADeletedCharacterHasMoreReputation() =>
            AddCharacterAsync(AuthorityType.User, SenderReputation * 10, CharacterState.Inactive);

        private void CheckingTheSender() =>
            Result = Service.IsTopThree(SenderId, SenderReputation, AuthorityType.User);

        private void CheckingAGameMaster() =>
            Result = Service.IsTopThree(SenderId, SenderReputation, AuthorityType.GameMaster);

        private void SenderShouldBeTopThree() => Assert.IsTrue(Result);

        private void SenderShouldNotBeTopThree() => Assert.IsFalse(Result);

        private async Task AddCharacterAsync(AuthorityType authority, long reputation, CharacterState state)
        {
            var id = NextId++;
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(new AccountDto
            {
                AccountId = id,
                Name = "RankedAccount" + id,
                Password = new Sha512Hasher().Hash("test"),
                Authority = authority
            });
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(new CharacterDto
            {
                CharacterId = id,
                AccountId = id,
                Name = "Ranked" + id,
                Slot = 1,
                MapId = 1,
                State = state,
                Reput = reputation,
                ServerId = TestHelpers.Instance.WorldConfiguration.Value.ServerId
            });
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.TimeSpace;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.TimeSpace
{
    [TestClass]
    public class TimeSpaceSucceededHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private TimeSpaceSucceededHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _handler = World.Create<TimeSpaceSucceededHandler>();
        }

        [TestMethod]
        public async Task ClearingATimeSpaceIsRecordedForTheCharacter()
        {
            await new Spec("Clearing a time-space records it for the character, in memory and in the database")
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .Then(TheCharacterShouldHaveCompleted_, new short[] { 6 })
                .And(TheDatabaseShouldHold_, new short[] { 6 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClearingATimeSpaceAgainKeepsOneRecord()
        {
            await new Spec("A time-space cleared twice is recorded once")
                .GivenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .Then(TheDatabaseShouldHold_, new short[] { 6 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EachClearedTimeSpaceIsRecorded()
        {
            await new Spec("Every time-space the character clears is recorded")
                .GivenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)5)
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .Then(TheCharacterShouldHaveCompleted_, new short[] { 5, 6 })
                .And(TheDatabaseShouldHold_, new short[] { 5, 6 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWonRaidIsNotATimeSpace()
        {
            await new Spec("Winning a raid records no time-space")
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.Raid, (short)6)
                .Then(TheCharacterShouldHaveCompleted_, new short[0])
                .And(TheDatabaseShouldHold_, new short[0])
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClearingATimeSpaceStillEndsTheLoserModeOffer()
        {
            await new Spec("Clearing a time-space still starts the failure counts over")
                .Given(TheCharacterFailedTimeSpace_Twice, (short)6)
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .Then(LoserModeShouldNotBeOfferedFor_, (short)6)
                .ExecuteAsync();
        }

        private Task The_Of_Succeeds(ScriptedInstanceType type, short id) =>
            _handler.Handle(new ScriptedInstanceSucceededEvent(_session.Character, type, id));

        private void TheCharacterFailedTimeSpace_Twice(short id)
        {
            _session.Character.RecordTimeSpaceFailure(id);
            _session.Character.RecordTimeSpaceFailure(id);
        }

        private void LoserModeShouldNotBeOfferedFor_(short id) => Assert.IsFalse(_session.Character.OffersLoserMode(id));

        private void TheCharacterShouldHaveCompleted_(short[] ids) =>
            CollectionAssert.AreEquivalent(ids, _session.Character.CompletedTimeSpaces?.ToArray() ?? []);

        private void TheDatabaseShouldHold_(short[] ids)
        {
            var characterId = _session.Character.CharacterId;
            var stored = World.CompletedTimeSpaceDao.Where(s => s.CharacterId == characterId)?.Select(s => s.TimeSpaceId).ToArray() ?? [];
            CollectionAssert.AreEquivalent(ids, stored);
        }
    }
}

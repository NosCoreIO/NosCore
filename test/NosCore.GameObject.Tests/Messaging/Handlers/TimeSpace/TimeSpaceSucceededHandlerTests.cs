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
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Messaging.Handlers.TimeSpace;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
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

        [TestMethod]
        public async Task AFirstClearGivesBackAHundredDignity()
        {
            await new Spec("Clearing a time-space for the first time gives back 100 dignity, announced, and moves the bar")
                .Given(TheCharacterHas_Dignity, (short)-152)
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .Then(TheCharacterShouldHave_Dignity, (short)-52)
                .And(TheGainOf_ShouldBeAnnounced, (short)100)
                .And(TheBarShouldShow_Dignity, (short)-52)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARepeatClearGivesBackFiftyDignity()
        {
            await new Spec("Clearing a time-space already cleared gives back 50 dignity")
                .Given(TheCharacterHas_Dignity, (short)-202)
                .And(TheCharacterHasCleared_, (short)6)
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .Then(TheCharacterShouldHave_Dignity, (short)-152)
                .And(TheGainOf_ShouldBeAnnounced, (short)50)
                .And(TheBarShouldShow_Dignity, (short)-152)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheGainStopsAtTheMaximum()
        {
            await new Spec("A clear near the maximum fills dignity to it and announces only what was added")
                .Given(TheCharacterHas_Dignity, (short)86)
                .And(TheCharacterHasCleared_, (short)6)
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .Then(TheCharacterShouldHave_Dignity, (short)100)
                .And(TheGainOf_ShouldBeAnnounced, (short)14)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AClearAtTheMaximumGivesNothing()
        {
            await new Spec("A clear with dignity already at the maximum announces no gain")
                .Given(TheCharacterHas_Dignity, (short)100)
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.TimeSpace, (short)6)
                .Then(TheCharacterShouldHave_Dignity, (short)100)
                .And(NoGainShouldBeAnnounced)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWonRaidGivesNoDignity()
        {
            await new Spec("Winning a raid is not a time-space clear and gives no dignity")
                .Given(TheCharacterHas_Dignity, (short)-152)
                .WhenAsync(The_Of_Succeeds, ScriptedInstanceType.Raid, (short)6)
                .Then(TheCharacterShouldHave_Dignity, (short)-152)
                .And(NoGainShouldBeAnnounced)
                .ExecuteAsync();
        }

        [TestMethod]
        public void TheClearIsCountedAfterTheQuestsItCompletes()
        {
            var handlers = GameEventDispatcher.HandlerTypesOf<ScriptedInstanceSucceededEvent>().ToList();

            var quests = handlers.IndexOf(typeof(OnScriptedInstanceSucceededHandler));
            Assert.IsTrue(quests >= 0 && quests < handlers.IndexOf(typeof(TimeSpaceSucceededHandler)));
        }

        private void TheCharacterHas_Dignity(short dignity) => _session.Character.Dignity = dignity;

        private void TheCharacterHasCleared_(short id) => _session.Character.CompletedTimeSpaces = [id];

        private void TheCharacterShouldHave_Dignity(short dignity) => Assert.AreEqual(dignity, _session.Character.Dignity);

        private void TheGainOf_ShouldBeAnnounced(short amount) =>
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = SayColorType.Red,
                Message = Game18NConstString.DignityIncreased,
                ArgumentType = 4,
                Game18NArguments = { amount }
            }, _session.LastPackets.OfType<SayiPacket>().Single(p => p.Message == Game18NConstString.DignityIncreased));

        private void NoGainShouldBeAnnounced() =>
            Assert.IsFalse(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.DignityIncreased));

        private void TheBarShouldShow_Dignity(short dignity) =>
            Assert.AreEqual(dignity, _session.LastPackets.OfType<FdPacket>().Last().Dignity);

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

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceRecordTests
    {
        private const short TimeSpaceId = 16;
        private const short OtherTimeSpaceId = 17;

        private TestWorld World;
        private ClientSession _session;
        private ClientSession _other;
        private GameObject.Services.ScriptedInstanceService.ScriptedInstance _entrance;
        private bool _broken;

        private sealed class Script : InstanceScript
        {
            public Script(short id) => Definition = InstanceDefinitionBuilder.Named(id, "timespace", "timespace").Build();
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.UseReal<ITimeSpaceRecordService, TimeSpaceRecordService>();
            _session = await World.CreateSessionAsync();
            _other = await World.CreateSessionAsync();
            _entrance = new GameObject.Services.ScriptedInstanceService.ScriptedInstance { Logic = new Script(TimeSpaceId) };
            World.Freeze<Mock<IScriptedInstanceService>>()
                .Setup(s => s.LoadAsync(_entrance)).ReturnsAsync(true);
        }

        [TestMethod]
        public async Task AStoneNobodyClearedShowsNoRecord()
        {
            await new Spec("A stone without a record shows a high score of 0 and no holder")
                .WhenAsync(TheStoneIsShown)
                .Then(TheStoneShouldShow_HeldBy_, (short)0, (string?)null)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStoneShowsItsRecordAndWhoSetIt()
        {
            await new Spec("A stone shows its best score with the name of the character who set it")
                .GivenAsync(TheOtherPlayerScored_In_, 1377, TimeSpaceId)
                .WhenAsync(TheStoneIsShown)
                .Then(TheStoneShouldShowTheOtherPlayersScore_, (short)1377)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARecordBelongsToItsTimeSpace()
        {
            await new Spec("A record set in another time-space is not shown on this stone")
                .GivenAsync(TheOtherPlayerScored_In_, 1377, OtherTimeSpaceId)
                .WhenAsync(TheStoneIsShown)
                .Then(TheStoneShouldShow_HeldBy_, (short)0, (string?)null)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHigherScoreBreaksTheRecord()
        {
            await new Spec("A score above the record replaces it with the new score and holder")
                .GivenAsync(TheOtherPlayerScored_In_, 1377, TimeSpaceId)
                .WhenAsync(ThePlayerScores_, 1500)
                .Then(TheRecordShouldBeBroken_, true)
                .And(TheRecordShouldBe_HeldByThePlayer_, 1500, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEqualScoreDoesNotBreakTheRecord()
        {
            await new Spec("A score equal to the record leaves it with its holder")
                .GivenAsync(TheOtherPlayerScored_In_, 1377, TimeSpaceId)
                .WhenAsync(ThePlayerScores_, 1377)
                .Then(TheRecordShouldBeBroken_, false)
                .And(TheRecordShouldBe_HeldByThePlayer_, 1377, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AScoreOfZeroSetsNoRecord()
        {
            await new Spec("A score of 0 on a stone without a record sets none")
                .WhenAsync(ThePlayerScores_, 0)
                .Then(TheRecordShouldBeBroken_, false)
                .And(NoRecordShouldBeKept)
                .ExecuteAsync();
        }

        private async Task TheOtherPlayerScored_In_(int score, short timeSpaceId) =>
            await World.Create<ITimeSpaceRecordService>().TryBreakAsync(timeSpaceId, _other.Character.CharacterId, score);

        private async Task ThePlayerScores_(int score) =>
            _broken = await World.Create<ITimeSpaceRecordService>().TryBreakAsync(TimeSpaceId, _session.Character.CharacterId, score);

        private Task TheStoneIsShown() => World.Create<TimeSpaceEntryService>().ShowAsync(_session, _entrance);

        private void TheStoneShouldShow_HeldBy_(short score, string? holder) =>
            PacketAssert.AreEqual(new RbrSubPacketHighScore { Score = score, Nickname = holder },
                _session.LastPackets.OfType<RbrPacket>().Single().HighScore);

        private void TheStoneShouldShowTheOtherPlayersScore_(short score) =>
            TheStoneShouldShow_HeldBy_(score, _other.Character.Name);

        private void TheRecordShouldBeBroken_(bool broken) => Assert.AreEqual(broken, _broken);

        private void TheRecordShouldBe_HeldByThePlayer_(int score, bool heldByThePlayer)
        {
            var record = World.TimeSpaceRecordDao.Where(r => r.TimeSpaceId == TimeSpaceId)?.Single();
            long? holder = heldByThePlayer ? _session.Character.CharacterId : _other.Character.CharacterId;
            Assert.AreEqual(((int?)score, holder), (record?.Score, record?.CharacterId));
        }

        private void NoRecordShouldBeKept() =>
            Assert.IsFalse(World.TimeSpaceRecordDao.Where(r => r.TimeSpaceId == TimeSpaceId)?.Any() ?? false);
    }
}

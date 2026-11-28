//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ScriptedInstanceService;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.Instances.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InstantCombatService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using InstantCombat = NosCore.GameObject.Services.InstantCombatService.InstantCombatService;

namespace NosCore.GameObject.Tests.Services.InstantCombatService
{
    [TestClass]
    public class InstantCombatServiceTests
    {
        private static readonly byte[] Brackets = [1, 40, 50, 60, 70, 80];

        private TestWorld World;
        private readonly List<ClientSession> _players = new();
        private List<InstantCombatRoom> _rooms = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
        }

        [TestMethod]
        public async Task PlayersFightWithTheirLevelBracket()
        {
            await new Spec("Each player fights in the highest bracket their level reaches")
                .GivenAsync(PlayersOfLevels_, new byte[] { 10, 39, 45, 85 })
                .When(GroupingThePlayers)
                .Then(TheRoomsShouldBe_, "1:10,39 40:45 80:85")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABracketSplitsPastFiftyPlayers()
        {
            await new Spec("A bracket with more than 50 players opens another room")
                .GivenAsync(PlayersOfLevels_, Enumerable.Repeat((byte)20, 51).ToArray())
                .When(GroupingThePlayers)
                .Then(TheRoomSizesShouldBe_, "50 1")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARunningCombatIsNotStartedAgain()
        {
            await new Spec("A combat already running on the channel is not started a second time")
                .Given(TheEventHasAScriptAndABracket)
                .When(StartingACombat)
                .Then(StartingAnotherShouldBeRefused)
                .AndAsync(TheRunShouldWaitForTheClock)
                .AndAsync(TheRunShouldEndOnceSignUpCloses)
                .ExecuteAsync();
        }

        // Without both, the run returns at once and can end before the second start.
        private void TheEventHasAScriptAndABracket()
        {
            World.Freeze<Mock<IScriptedInstanceService>>()
                .Setup(s => s.LoadScriptAsync(InstantCombat.Script))
                .ReturnsAsync(new Mock<IInstanceScript>().Object);
            World.Inject(new List<InstantCombatMonsterDto> { new() { LevelMinimum = 1, Wave = 1, NpcMonsterVNum = 1, Amount = 1 } });
        }

        private async Task TheRunShouldWaitForTheClock()
        {
            await Task.Delay(100);
            Assert.IsTrue(_service.IsRunning);
        }

        private async Task TheRunShouldEndOnceSignUpCloses()
        {
            // The sign-up wait may not have started yet, so keep moving the clock until it has.
            for (var i = 0; i < 200 && _service.IsRunning; i++)
            {
                World.Clock.AdvanceSeconds(30);
                await Task.Delay(10);
            }
            Assert.IsFalse(_service.IsRunning);
        }

        private async Task PlayersOfLevels_(byte[] levels)
        {
            foreach (var level in levels)
            {
                var session = await World.CreateSessionAsync();
                session.Character.Level = level;
                _players.Add(session);
            }
        }

        private void GroupingThePlayers() => _rooms = InstantCombat.GroupIntoRooms(_players, Brackets).ToList();

        private void TheRoomsShouldBe_(string expected) =>
            Assert.AreEqual(expected, string.Join(" ", _rooms.Select(r =>
                $"{r.LevelMinimum}:{string.Join(",", r.Players.Select(p => p.Character.Level))}")));

        private void TheRoomSizesShouldBe_(string expected) =>
            Assert.AreEqual(expected, string.Join(" ", _rooms.Select(r => r.Players.Count)));

        private IInstantCombatService _service;

        private void StartingACombat()
        {
            _service = World.Create<InstantCombat>();
            Assert.IsTrue(_service.TryStart(false));
        }

        private void StartingAnotherShouldBeRefused() => Assert.IsFalse(_service.TryStart(false));

        [TestMethod]
        public async Task AFailingRunIsCountedAndFreesTheChannel()
        {
            using var failures = new FailureCounter("event_run");
            _failures = failures;
            await new Spec("A run that throws is counted, and the channel can start an Instant Combat again")
                .Given(TheEventScriptCannotBeLoaded)
                .WhenAsync(ARunFails)
                .Then(TheFailureShouldBeCounted)
                .AndAsync(AnotherRunCanStartAndEnd)
                .ExecuteAsync();
        }

        private FailureCounter _failures;
        private InstantCombat _failing;

        private void TheEventScriptCannotBeLoaded()
        {
            World.Freeze<Mock<IScriptedInstanceService>>()
                .Setup(s => s.LoadScriptAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException());
            _failing = World.Create<InstantCombat>();
        }

        private async Task ARunFails()
        {
            Assert.IsTrue(_failing.TryStart(false));
            await TheRunEndsAsync();
        }

        private async Task TheRunEndsAsync()
        {
            for (var i = 0; i < 200 && _failing.IsRunning; i++)
            {
                await Task.Delay(10);
            }
            Assert.IsFalse(_failing.IsRunning);
        }

        private void TheFailureShouldBeCounted() => Assert.IsTrue(_failures.Count >= 1);

        private async Task AnotherRunCanStartAndEnd()
        {
            Assert.IsTrue(_failing.TryStart(false));
            await TheRunEndsAsync();
        }
    

        private const short PrizeVNum = 1015;
        private ClientSession _joiner;
        private ClientSession _absent;

        [TestMethod]
        public async Task AWavesPrizeGoesStraightIntoEveryPlayersBag()
        {
            await new Spec("A wave's prize goes into the bag of each player in the room, with nothing left on the map")
                .GivenAsync(TwoPlayersInTheRoom)
                .And(TheBracketsFirstWaveGives_Of_, (short)5, PrizeVNum)
                .WhenAsync(TheFirstWavesPrizeIsHandedOut)
                .Then(EachPlayerShouldHold_, 5)
                .And(EachPlayerShouldBeToldOfThePrize)
                .And(NothingShouldLieOnTheMap)
                .ExecuteAsync();
        }

        private async Task TwoPlayersInTheRoom()
        {
            _joiner = await World.CreateSessionAsync();
            _absent = await World.CreateSessionAsync();
        }

        private void TheBracketsFirstWaveGives_Of_(short amount, short vnum) => World.Inject(new List<InstantCombatDropDto>
        {
            new() { InstantCombatDropId = 1, LevelMinimum = 1, Wave = 0, ItemVNum = vnum, Amount = amount },
            new() { InstantCombatDropId = 2, LevelMinimum = 1, Wave = 1, ItemVNum = vnum, Amount = 7 },
            new() { InstantCombatDropId = 3, LevelMinimum = 40, Wave = 0, ItemVNum = vnum, Amount = 9 },
        });

        private Task TheFirstWavesPrizeIsHandedOut() =>
            World.Create<InstantCombat>().GiveWavePrizeAsync(World.GetBaseMap(TestWorld.TestMapId), 1, 0);

        private void EachPlayerShouldHold_(int amount)
        {
            Assert.AreEqual(amount, _joiner.Character.InventoryService.CountItem(PrizeVNum));
            Assert.AreEqual(amount, _absent.Character.InventoryService.CountItem(PrizeVNum));
        }

        private void EachPlayerShouldBeToldOfThePrize()
        {
            foreach (var session in new[] { _joiner, _absent })
            {
                PacketAssert.AreEqual(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ReceivedThisItem,
                    ArgumentType = 2,
                    Game18NArguments = { PrizeVNum.ToString(), (short)5 },
                }, session.LastPackets.OfType<SayiPacket>().Single());
            }
        }

        private void NothingShouldLieOnTheMap() =>
            Assert.AreEqual(0, World.GetBaseMap(TestWorld.TestMapId).MapItemCount);

        [TestMethod]
        public async Task WhoeverHasNotSignedUpIsRemindedAndThenToldTheSignUpIsOver()
        {
            await new Spec("The start is shown again every seven seconds to whoever has not signed up, and the sign-up closes on them alone")
                .GivenAsync(TwoPlayersInTheRoom)
                .And(TheEventHasAScriptAndABracket)
                .WhenAsync(OneSignsUpAndTheOtherWaitsSevenSeconds)
                .Then(OnlyTheOtherShouldSeeTheStartAgain)
                .AndAsync(OnlyTheOtherShouldBeToldTheSignUpIsOver)
                .ExecuteAsync();
        }

        private static int Starts(ClientSession session) => session.LastPackets.OfType<MsgiPacket>()
            .Count(p => p is { Type: MessageType.Notification, Message: Game18NConstString.InstantCombatStarted });

        private static async Task UntilAsync(Func<bool> condition)
        {
            for (var i = 0; i < 300 && !condition(); i++)
            {
                await Task.Delay(10);
            }

            Assert.IsTrue(condition());
        }

        private async Task OneSignsUpAndTheOtherWaitsSevenSeconds()
        {
            _service = World.Create<InstantCombat>();
            Assert.IsTrue(_service.TryStart(false));
            await UntilAsync(() => _absent.LastPackets.OfType<QnamliPacket>().Any());
            _service.SignUp(_joiner);
            await Task.Delay(50);
            World.Clock.AdvanceSeconds(7);
            await UntilAsync(() => Starts(_absent) == 2);
        }

        private void OnlyTheOtherShouldSeeTheStartAgain() => Assert.AreEqual(1, Starts(_joiner));

        private async Task OnlyTheOtherShouldBeToldTheSignUpIsOver()
        {
            World.Clock.AdvanceSeconds(30);
            await UntilAsync(() => !_service.IsRunning);
            PacketAssert.AreEqual(new EsfPacket { Value = 4 }, _absent.LastPackets.OfType<EsfPacket>().Single());
            Assert.IsFalse(_joiner.LastPackets.OfType<EsfPacket>().Any());
        }

        private ClientSession _winner;
        private long _goldBefore;
        private long _reputationBefore;

        [TestMethod]
        public async Task AWinnerIsToldOfEachRewardOnItsOwnLine()
        {
            await new Spec("A level 90 winner gets 90000 gold, 4500 reputation and 9000 SP points, each told on its own yellow line")
                .GivenAsync(AWinnerOfLevel_, (byte)90)
                .WhenAsync(TheWinnerIsRewarded)
                .Then(TheWinnerShouldHaveGained_GoldAnd_Reputation, 90000L, 4500L)
                .And(TheWinnerShouldBeToldOf_Gold_FameAnd_SpPoints, 90000L, 4500L, 9000L)
                .ExecuteAsync();
        }

        private async Task AWinnerOfLevel_(byte level)
        {
            _winner = await World.CreateSessionAsync();
            _winner.Character.Level = level;
            _goldBefore = _winner.Character.Gold;
            _reputationBefore = _winner.Character.Reputation;
        }

        private Task TheWinnerIsRewarded() => World.Create<InstantCombat>().RewardAsync(_winner.Character);

        private void TheWinnerShouldHaveGained_GoldAnd_Reputation(long gold, long reputation)
        {
            Assert.AreEqual(_goldBefore + gold, _winner.Character.Gold);
            Assert.AreEqual(_reputationBefore + reputation, _winner.Character.Reputation);
        }

        private void TheWinnerShouldBeToldOf_Gold_FameAnd_SpPoints(long gold, long fame, long sp)
        {
            var lines = _winner.LastPackets.OfType<SayiPacket>().Where(p => p.Type == SayColorType.Yellow)
                .Select(p => $"{p.Message} {p.ArgumentType} {p.Game18NArguments.First()}");
            Assert.AreEqual(string.Join(" | ", new[]
            {
                $"{Game18NConstString.VictoryReceivedGold} 4 {gold}",
                $"{Game18NConstString.VictoryReceivedFame} 4 {fame}",
                $"{Game18NConstString.VictoryReceivedAdditionalSP} 4 {sp}",
            }), string.Join(" | ", lines));
            Assert.IsFalse(_winner.LastPackets.OfType<MsgiPacket>().Any());
        }

        [TestMethod]
        public async Task LeavingClearsTheEventScreenBeforeTheWayBack()
        {
            await new Spec("A player sent back from the combat first has the event's screen cleared")
                .GivenAsync(AWinnerOfLevel_, (byte)90)
                .WhenAsync(TheWinnerIsSentBack)
                .Then(TheFirstPacketShouldClearTheEventScreen)
                .ExecuteAsync();
        }

        private Task TheWinnerIsSentBack()
        {
            _winner.LastPackets.Clear();
            return World.Create<InstantCombat>().ReturnAsync(_winner);
        }

        private void TheFirstPacketShouldClearTheEventScreen() =>
            PacketAssert.AreEqual(new EventPacket { Type = EventPacketType.ClearTimeSpaceUI }, _winner.LastPackets.First());
    }
}

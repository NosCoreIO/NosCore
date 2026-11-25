//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.TimeSpace;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceLoserModeTests
    {
        private const short TimeSpaceId = 7;
        private const short OtherTimeSpaceId = 8;

        private TestWorld World;
        private ClientSession _session;
        private GameObject.Services.ScriptedInstanceService.ScriptedInstance _entrance;
        private Script _script;

        private sealed class Script : InstanceScript
        {
            public readonly int Room;

            public Script(short id) => Definition = InstanceDefinitionBuilder.Named(id, "timespace", "timespace")
                .WithRoom(4100, out Room)
                .Build();
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _script = new Script(TimeSpaceId);
            _entrance = new GameObject.Services.ScriptedInstanceService.ScriptedInstance { Logic = _script };
            World.Freeze<Mock<IMapChangeService>>();
            var instances = World.Freeze<Mock<IScriptedInstanceService>>();
            instances.Setup(s => s.LoadAsync(_entrance)).ReturnsAsync(true);
            instances.Setup(s => s.InstantiateAsync(_entrance)).ReturnsAsync(CreateRun);
        }

        [TestMethod]
        public async Task TheSecondFailureOffersLoserModeAsync()
        {
            await new Spec("A second failed attempt tells the player Loser Mode is available, and the stone offers it")
                .GivenAsync(ThePlayerFailed_Times, TimeSpaceId, 1)
                .WhenAsync(ThePlayerFails_, TimeSpaceId)
                .Then(ThePlayerShouldBeToldLoserModeIsAvailableAfter_Failures, 2)
                .AndAsync(TheStoneShould_OfferLoserMode, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EachFurtherFailureRaisesTheCountAsync()
        {
            await new Spec("Failing again after Loser Mode is offered tells the player the new count, and the stone still offers it")
                .GivenAsync(ThePlayerFailed_Times, TimeSpaceId, 3)
                .WhenAsync(ThePlayerFails_, TimeSpaceId)
                .Then(ThePlayerShouldBeToldLoserModeIsAvailableAfter_Failures, 4)
                .AndAsync(TheStoneShould_OfferLoserMode, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoserModeEntryNeedsTheFailuresAsync()
        {
            await new Spec("Asking to enter in Loser Mode without enough failed attempts opens no run")
                .GivenAsync(ThePlayerFailed_Times, TimeSpaceId, 1)
                .WhenAsync(ThePlayerEntersInLoserMode)
                .Then(ARunShouldHaveBeenOpened_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoserModeEntryOpensARunAfterTwoFailuresAsync()
        {
            await new Spec("Asking to enter in Loser Mode after two failed attempts opens a run")
                .GivenAsync(ThePlayerFailed_Times, TimeSpaceId, 2)
                .WhenAsync(ThePlayerEntersInLoserMode)
                .Then(ARunShouldHaveBeenOpened_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoserModeEntryCostsDignityAndWarnsAboutTheRewardAsync()
        {
            await new Spec("Entering in Loser Mode warns that the reward may be limited and takes 10 dignity")
                .GivenAsync(ThePlayerFailed_Times, TimeSpaceId, 2)
                .And(ThePlayerHas_Dignity, (short)50)
                .WhenAsync(ThePlayerEntersInLoserMode)
                .Then(ThePlayerShouldBeWarnedTheRewardMayBeLimited_, true)
                .And(ThePlayerShouldHave_Dignity, (short)40)
                .And(ThePlayerShouldBeToldTheDignityChangedBy_, -10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANormalEntryCostsNoDignityAsync()
        {
            await new Spec("Entering normally after two failures neither warns about the reward nor takes dignity")
                .GivenAsync(ThePlayerFailed_Times, TimeSpaceId, 2)
                .And(ThePlayerHas_Dignity, (short)50)
                .WhenAsync(ThePlayerEntersNormally)
                .Then(ThePlayerShouldBeWarnedTheRewardMayBeLimited_, false)
                .And(ThePlayerShouldHave_Dignity, (short)50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OneFailureIsNotEnoughAsync()
        {
            await new Spec("A single failed attempt does not offer Loser Mode")
                .WhenAsync(ThePlayerFails_, TimeSpaceId)
                .Then(ThePlayerShouldNotBeToldAboutLoserMode)
                .AndAsync(TheStoneShould_OfferLoserMode, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FailuresAreCountedPerTimeSpaceAsync()
        {
            await new Spec("Failures in another time-space do not offer Loser Mode in this one")
                .GivenAsync(ThePlayerFailed_Times, OtherTimeSpaceId, 2)
                .ThenAsync(TheStoneShould_OfferLoserMode, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASuccessClearsTheFailuresAsync()
        {
            await new Spec("Clearing the time-space stops offering Loser Mode")
                .GivenAsync(ThePlayerFailed_Times, TimeSpaceId, 2)
                .When(ThePlayerClears_, TimeSpaceId)
                .ThenAsync(TheStoneShould_OfferLoserMode, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClearingAnotherTimeSpaceAlsoClearsTheFailuresAsync()
        {
            await new Spec("Clearing any other time-space starts the failed attempts over")
                .GivenAsync(ThePlayerFailed_Times, TimeSpaceId, 2)
                .When(ThePlayerClears_, OtherTimeSpaceId)
                .ThenAsync(TheStoneShould_OfferLoserMode, false)
                .ExecuteAsync();
        }

        private async Task ThePlayerFailed_Times(short timeSpaceId, int failures)
        {
            for (var i = 0; i < failures; i++)
            {
                await ThePlayerFails_(timeSpaceId);
            }
        }

        private Task ThePlayerFails_(short timeSpaceId)
        {
            _session.LastPackets.Clear();
            return World.Create<TimeSpaceFailedHandler>().Handle(new TimeSpaceFailedEvent(_session.Character, timeSpaceId));
        }

        private void ThePlayerClears_(short timeSpaceId) =>
            World.Create<TimeSpaceSucceededHandler>().Handle(
                new ScriptedInstanceSucceededEvent(_session.Character, ScriptedInstanceType.TimeSpace, timeSpaceId));

        private void ThePlayerShouldBeToldLoserModeIsAvailableAfter_Failures(int failures)
        {
            var told = _session.LastPackets.OfType<SayiPacket>()
                .Single(packet => packet.Message == Game18NConstString.LoserModeAvailable);
            Assert.AreEqual(SayColorType.Red, told.Type);
            Assert.AreEqual(failures, Convert.ToInt32(told.Game18NArguments.Single()));
        }

        private Task ThePlayerEntersInLoserMode()
        {
            _session.LastPackets.Clear();
            return World.Create<TimeSpaceEntryService>().EnterAsync(_session, _entrance, loserMode: true);
        }

        private Task ThePlayerEntersNormally()
        {
            _session.LastPackets.Clear();
            return World.Create<TimeSpaceEntryService>().EnterAsync(_session, _entrance);
        }

        private void ThePlayerHas_Dignity(short dignity) => _session.Character.Dignity = dignity;

        private void ThePlayerShouldHave_Dignity(short dignity) => Assert.AreEqual(dignity, _session.Character.Dignity);

        private void ThePlayerShouldBeToldTheDignityChangedBy_(int change)
        {
            var say = _session.LastPackets.OfType<SayiPacket>().Single(packet => packet.Message == Game18NConstString.DignityReduced);
            Assert.AreEqual(change, Convert.ToInt32(say.Game18NArguments.Single()));
        }

        private void ThePlayerShouldBeWarnedTheRewardMayBeLimited_(bool warned)
        {
            var warnings = _session.LastPackets.OfType<SayiPacket>()
                .Where(packet => packet.Message == Game18NConstString.RewardLimitedObserverMode).ToList();
            Assert.AreEqual(warned ? 1 : 0, warnings.Count);
            Assert.IsTrue(warnings.All(packet => packet.Type == SayColorType.Yellow));
        }

        private void ARunShouldHaveBeenOpened_Times(int times) =>
            World.Freeze<Mock<IScriptedInstanceService>>().Verify(s => s.InstantiateAsync(_entrance), Times.Exactly(times));

        private void ThePlayerShouldNotBeToldAboutLoserMode()
        {
            Assert.IsFalse(_session.LastPackets.OfType<SayiPacket>().Any(packet => packet.Message == Game18NConstString.LoserModeAvailable));
        }

        private ScriptedInstanceRun CreateRun()
        {
            var room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 4100,
                    NameI18NKey = "room",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            room.Sessions = new Mock<ISessionGroup>().Object;
            return new ScriptedInstanceRun(
                World.CreateTimeSpaceRunHost(_entrance),
                _script,
                new Dictionary<int, MapInstance> { [_script.Room] = room },
                new NpcMonsterCatalog([]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
        }

        private async Task TheStoneShould_OfferLoserMode(bool offered)
        {
            _session.LastPackets.Clear();
            await World.Create<TimeSpaceEntryService>().ShowAsync(_session, _entrance);
            Assert.AreEqual(offered, _session.LastPackets.OfType<RbrPacket>().Single().LoserMode);
        }
    }
}

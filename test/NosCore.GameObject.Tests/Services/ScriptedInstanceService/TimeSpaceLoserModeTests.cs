//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.TimeSpace;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Tests.Shared;
using SpecLight;
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

        private sealed class Script : InstanceScript
        {
            public Script(short id) => Definition = InstanceDefinitionBuilder.Named(id, "timespace", "timespace").Build();
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            World.Inject<ITimeSpaceAttemptService>(World.Create<TimeSpaceAttemptService>());
            _entrance = new GameObject.Services.ScriptedInstanceService.ScriptedInstance { Logic = new Script(TimeSpaceId) };
            World.Freeze<Mock<IScriptedInstanceService>>()
                .Setup(s => s.LoadAsync(_entrance)).ReturnsAsync(true);
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
                .WhenAsync(ThePlayerClears_, TimeSpaceId)
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

        private Task ThePlayerClears_(short timeSpaceId) =>
            World.Create<TimeSpaceSucceededHandler>().Handle(
                new ScriptedInstanceSucceededEvent(_session.Character, ScriptedInstanceType.TimeSpace, timeSpaceId));

        private void ThePlayerShouldBeToldLoserModeIsAvailableAfter_Failures(int failures)
        {
            var told = _session.LastPackets.OfType<SayiPacket>()
                .Single(packet => packet.Message == Game18NConstString.LoserModeAvailable);
            Assert.AreEqual(SayColorType.Red, told.Type);
            Assert.AreEqual(failures, System.Convert.ToInt32(told.Game18NArguments.Single()));
        }

        private void ThePlayerShouldNotBeToldAboutLoserMode()
        {
            Assert.IsFalse(_session.LastPackets.OfType<SayiPacket>().Any(packet => packet.Message == Game18NConstString.LoserModeAvailable));
        }

        private async Task TheStoneShould_OfferLoserMode(bool offered)
        {
            _session.LastPackets.Clear();
            await World.Create<TimeSpaceEntryService>().ShowAsync(_session, _entrance);
            Assert.AreEqual(offered, _session.LastPackets.OfType<RbrPacket>().Single().LoserMode);
        }
    }
}

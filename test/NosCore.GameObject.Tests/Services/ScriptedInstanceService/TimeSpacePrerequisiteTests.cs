//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpacePrerequisiteTests
    {
        private const short TimeSpaceId = 9;
        private const short RequiredTimeSpaceId = 6;

        private TestWorld World;
        private ClientSession _session;
        private GameObject.Services.ScriptedInstanceService.ScriptedInstance _entrance;
        private Mock<IScriptedInstanceService> _scriptedInstances;

        private sealed class Script : InstanceScript
        {
            public Script() => Definition = InstanceDefinitionBuilder.Named(TimeSpaceId, "timespace", "timespace")
                .RequiringCleared(RequiredTimeSpaceId)
                .Build();
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _entrance = new GameObject.Services.ScriptedInstanceService.ScriptedInstance { Logic = new Script() };
            _scriptedInstances = World.Freeze<Mock<IScriptedInstanceService>>();
            _scriptedInstances.Setup(s => s.LoadAsync(_entrance)).ReturnsAsync(true);
        }

        [TestMethod]
        public async Task AStoneWhosePrerequisiteIsNotClearedShowsLocked()
        {
            await new Spec("Time-space 9's stone shows 5 in its state field while time-space 6 is not cleared")
                .WhenAsync(TheStoneIsShown)
                .Then(TheStoneStateShouldBe_, (byte)5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStoneWhosePrerequisiteIsClearedShowsOpen()
        {
            await new Spec("Time-space 9's stone shows 1 in its state field once time-space 6 is cleared")
                .Given(TheCharacterHasCleared_, new[] { RequiredTimeSpaceId })
                .WhenAsync(TheStoneIsShown)
                .Then(TheStoneStateShouldBe_, (byte)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringWithoutThePrerequisiteIsRefused()
        {
            await new Spec("Entering time-space 9 before clearing time-space 6 is refused with the client's own line and opens no run")
                .WhenAsync(ThePlayerEnters)
                .Then(ThePlayerShouldBeToldSomeStonesAreNotDone_, true)
                .And(ARunShouldHaveBeenOpened_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringWithThePrerequisiteOpensARun()
        {
            await new Spec("Entering time-space 9 after clearing time-space 6 opens a run")
                .Given(TheCharacterHasCleared_, new[] { RequiredTimeSpaceId })
                .WhenAsync(ThePlayerEnters)
                .Then(ThePlayerShouldBeToldSomeStonesAreNotDone_, false)
                .And(ARunShouldHaveBeenOpened_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClearingAnotherTimeSpaceDoesNotUnlockIt()
        {
            await new Spec("Clearing a time-space other than 6 still leaves time-space 9 shut")
                .Given(TheCharacterHasCleared_, new short[] { 5 })
                .WhenAsync(ThePlayerEnters)
                .Then(ThePlayerShouldBeToldSomeStonesAreNotDone_, true)
                .ExecuteAsync();
        }

        private void TheCharacterHasCleared_(short[] ids) =>
            _session.Character.CompletedTimeSpaces = [.. ids];

        private Task TheStoneIsShown() => World.Create<TimeSpaceEntryService>().ShowAsync(_session, _entrance);

        private Task ThePlayerEnters() => World.Create<TimeSpaceEntryService>().EnterAsync(_session, _entrance);

        private void TheStoneStateShouldBe_(byte state) =>
            Assert.AreEqual(state, _session.LastPackets.OfType<RbrPacket>().Single().Unknown);

        private void ThePlayerShouldBeToldSomeStonesAreNotDone_(bool told) =>
            Assert.AreEqual(told, _session.LastPackets.OfType<MsgiPacket>()
                .Any(m => m.Message == Game18NConstString.SomeTimeSpaceStonesNotDone && m.Type == MessageType.Default));

        private void ARunShouldHaveBeenOpened_Times(int times) =>
            _scriptedInstances.Verify(s => s.InstantiateAsync(_entrance), Times.Exactly(times));
    }
}

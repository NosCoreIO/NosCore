//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class DelayExtensionsTests
    {
        private TestWorld World;
        private readonly Guid _vehicle = Guid.NewGuid();
        private readonly Guid _specialist = Guid.NewGuid();
        private ClientSession _session;
        private bool _completed;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task ABarCompletesForTheTargetItWasStartedFor()
        {
            await new Spec("A three-second bar completes for its own target once it has run")
                .GivenAsync(ALocomotionBarStartsFor_, _vehicle)
                .And(Milliseconds_Pass, 3000)
                .When(CompletingTheLocomotionBarFor_, _vehicle)
                .Then(TheBarShouldComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABarOfTheSameTypeDoesNotCompleteForAnotherTarget()
        {
            await new Spec("A vehicle's bar cannot be completed as a specialist transformation")
                .GivenAsync(ALocomotionBarStartsFor_, _vehicle)
                .And(Milliseconds_Pass, 5000)
                .When(CompletingTheLocomotionBarFor_, _specialist)
                .Then(TheBarShouldNotComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACancelledBarDoesNotComplete()
        {
            await new Spec("A bar cancelled part-way through does not complete afterwards")
                .GivenAsync(ALocomotionBarStartsFor_, _vehicle)
                .And(TheBarIsCancelled)
                .And(Milliseconds_Pass, 3000)
                .When(CompletingTheLocomotionBarFor_, _vehicle)
                .Then(TheBarShouldNotComplete)
                .ExecuteAsync();
        }

        private Task ALocomotionBarStartsFor_(Guid target)
        {
            return _session.Character.StartDelayAsync(new DelayPacket
            {
                Delay = 3000,
                Type = DelayPacketType.Locomotion,
            }, target, World.Clock.GetCurrentInstant());
        }

        private void Milliseconds_Pass(int milliseconds) => World.Clock.AdvanceMilliseconds(milliseconds);

        private void TheBarIsCancelled() => _session.Character.CancelDelay();

        private void CompletingTheLocomotionBarFor_(Guid target)
        {
            _completed = _session.Character.TryCompleteDelay(DelayPacketType.Locomotion, target,
                World.Clock.GetCurrentInstant());
        }

        private void TheBarShouldComplete() => Assert.IsTrue(_completed);

        private void TheBarShouldNotComplete() => Assert.IsFalse(_completed);
    }
}

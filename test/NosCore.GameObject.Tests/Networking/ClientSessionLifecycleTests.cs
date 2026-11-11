//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.GameObject.Services.SaveService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class ClientSessionLifecycleTests
    {
        private TestWorld World;
        private ClientSession Session;
        private RecordingPulseHandler PulseHandler;
        private Mock<ISessionDisconnectHandler> DisconnectHandler;
        private readonly List<string> Events = new();
        private ClientSession.PacketLockScope? HeldPacketLock;
        private Task DisconnectTask;
        private Task QueuedPacketTask;
        private int ClaimedSessionId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Events.Clear();
            PulseHandler = new RecordingPulseHandler(Events);
            DisconnectHandler = new Mock<ISessionDisconnectHandler>();
            DisconnectHandler.Setup(h => h.HandleDisconnectAsync(It.IsAny<ClientSession>()))
                .Callback(() => Events.Add("save"))
                .Returns(Task.CompletedTask);
            World.PubSubHub.Reset();
            World.PubSubHub.Setup(h => h.SubscribeAsync(It.IsAny<Subscriber>())).ReturnsAsync(true);
            World.PubSubHub.Setup(h => h.UnsubscribeAsync(It.IsAny<long>()))
                .Callback(() => Events.Add("unsubscribe"))
                .ReturnsAsync(true);
            Session = await World.GenerateSessionAsync(
                new List<IPacketHandler> { PulseHandler },
                new List<ISessionDisconnectHandler> { DisconnectHandler.Object });
            Session.LastKeepAliveIdentity = 1;
            ClaimedSessionId = Session.SessionId;
        }

        [TestMethod]
        public async Task SessionThatNeverClaimedAnAccountDoesNotUnsubscribe()
        {
            await new Spec("Session that never claimed an account does not unsubscribe")
                .WhenAsync(TheSessionDisconnects)
                .Then(NothingShouldBeUnsubscribed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClaimedSessionUnsubscribesTheIdItClaimed()
        {
            await new Spec("Claimed session unsubscribes the id it claimed")
                .GivenAsync(TheSessionClaimedItsAccount)
                .And(TheSessionIdChangesAfterwards)
                .WhenAsync(TheSessionDisconnects)
                .Then(OnlyTheClaimedIdShouldBeUnsubscribed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClaimedSessionUnsubscribesAfterTheDisconnectSave()
        {
            await new Spec("Claimed session unsubscribes after the disconnect save")
                .GivenAsync(TheSessionClaimedItsAccount)
                .WhenAsync(TheSessionDisconnects)
                .Then(SaveShouldComeBeforeUnsubscribe)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketErrorDoesNotUnsubscribeBeforeTheDisconnectSave()
        {
            await new Spec("Packet error does not unsubscribe before the disconnect save")
                .GivenAsync(TheSessionClaimedItsAccount)
                .And(ThePulseHandlerThrows)
                .WhenAsync(APulseArrivesFromTheNetwork)
                .Then(NothingShouldBeUnsubscribed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DisconnectWaitsForThePacketInFlight()
        {
            await new Spec("Disconnect waits for the packet in flight")
                .GivenAsync(APacketHoldsTheSessionLock)
                .When(TheSessionStartsDisconnecting)
                .Then(TheDisconnectSaveShouldNotHaveRun)
                .AndAsync(ThePacketFinishes)
                .And(TheDisconnectSaveShouldHaveRun)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DisconnectHandlerCanReuseTheDisconnectLock()
        {
            await new Spec("Disconnect handler can reuse the disconnect lock")
                .Given(TheDisconnectSaveTakesThePacketLockUnlessHeld)
                .WhenAsync(TheSessionDisconnectsWithinFiveSeconds)
                .Then(TheDisconnectSaveShouldHaveRun)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDisconnectSaveRunsUnderTheDisconnectLock()
        {
            await new Spec("The save run on disconnect completes while the disconnect holds the packet lock")
                .Given(TheDisconnectRunsTheSaveHandler)
                .WhenAsync(TheSessionDisconnectsWithinFiveSeconds)
                .Then(TheDisconnectSaveShouldHaveRun)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketQueuedBeforeTheDisconnectRunsBeforeTheSave()
        {
            await new Spec("Packet queued before the disconnect runs before the save")
                .GivenAsync(APacketHoldsTheSessionLock)
                .And(APulseIsQueuedFromTheNetwork)
                .And(TheSessionStartsDisconnecting)
                .WhenAsync(ThePacketFinishes)
                .Then(ThePulseShouldComeBeforeTheSave)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketQueuedBehindTheDisconnectIsSkipped()
        {
            await new Spec("Packet queued behind the disconnect is skipped")
                .GivenAsync(APacketHoldsTheSessionLock)
                .And(TheSessionStartsDisconnecting)
                .And(APulseIsQueuedFromTheNetwork)
                .WhenAsync(ThePacketFinishes)
                .Then(TheQueuedPulseShouldNotHaveRun)
                .ExecuteAsync();
        }

        private async Task TheSessionClaimedItsAccount()
        {
            Assert.IsTrue(await Session.ClaimAccountAsync(Session.Account));
        }

        [TestMethod]
        public async Task FailedUnsubscribeStillReleasesThePacketLock()
        {
            await new Spec("Failed unsubscribe still releases the packet lock")
                .GivenAsync(TheSessionClaimedItsAccount)
                .And(TheMasterFailsToUnsubscribe)
                .WhenAsync(TheSessionDisconnects)
                .ThenAsync(ThePacketLockShouldBeFree)
                .ExecuteAsync();
        }

        private void TheMasterFailsToUnsubscribe() =>
            World.PubSubHub.Setup(h => h.UnsubscribeAsync(It.IsAny<long>()))
                .ThrowsAsync(new TimeoutException());

        private async Task ThePacketLockShouldBeFree()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var packetLock = await Session.AcquirePacketLockAsync(timeout.Token);
        }

        private void TheDisconnectSaveTakesThePacketLockUnlessHeld() =>
            DisconnectHandler.Setup(h => h.HandleDisconnectAsync(It.IsAny<ClientSession>()))
                .Returns(async () =>
                {
                    using (await Session.AcquirePacketLockUnlessHeldAsync())
                    {
                        Events.Add("save");
                    }
                });

        private void TheDisconnectRunsTheSaveHandler()
        {
            var saveService = new Mock<ISaveService>();
            saveService.Setup(s => s.SaveAsync(It.IsAny<ClientSession>()))
                .Callback(() => Events.Add("save"))
                .Returns(Task.CompletedTask);
            DisconnectHandler.Setup(h => h.HandleDisconnectAsync(It.IsAny<ClientSession>()))
                .Returns((ClientSession session) => new SaveDisconnectHandler(saveService.Object).HandleDisconnectAsync(session));
        }

        private async Task TheSessionDisconnectsWithinFiveSeconds() =>
            await Session.OnDisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));

        private void TheSessionIdChangesAfterwards() => Session.SessionId = ClaimedSessionId + 100;

        private void ThePulseHandlerThrows() => PulseHandler.Throws = true;

        private async Task TheSessionDisconnects() => await Session.OnDisconnectedAsync();

        private async Task APulseArrivesFromTheNetwork() =>
            await Session.HandlePacketAsync(new NosPackageInfo { Packets = new[] { Pulse() } });

        private async Task APacketHoldsTheSessionLock() => HeldPacketLock = await Session.AcquirePacketLockAsync();

        private void TheSessionStartsDisconnecting() => DisconnectTask = Session.OnDisconnectedAsync();

        private void APulseIsQueuedFromTheNetwork() => QueuedPacketTask = Session.HandlePacketsAsync(new[] { Pulse() }, true);

        private async Task ThePacketFinishes()
        {
            HeldPacketLock.NotNull().Dispose();
            await DisconnectTask.WaitAsync(TimeSpan.FromSeconds(5));
            if (QueuedPacketTask != null)
            {
                await QueuedPacketTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        private void NothingShouldBeUnsubscribed() =>
            World.PubSubHub.Verify(h => h.UnsubscribeAsync(It.IsAny<long>()), Times.Never);

        private void OnlyTheClaimedIdShouldBeUnsubscribed()
        {
            World.PubSubHub.Verify(h => h.UnsubscribeAsync(ClaimedSessionId), Times.Once);
            World.PubSubHub.Verify(h => h.UnsubscribeAsync(It.IsAny<long>()), Times.Once);
        }

        private void SaveShouldComeBeforeUnsubscribe() =>
            CollectionAssert.AreEqual(new List<string> { "save", "unsubscribe" }, Events);

        private void TheDisconnectSaveShouldNotHaveRun() => Assert.IsFalse(Events.Contains("save"));

        private void TheDisconnectSaveShouldHaveRun() => Assert.IsTrue(Events.Contains("save"));

        private void ThePulseShouldComeBeforeTheSave() =>
            CollectionAssert.AreEqual(new List<string> { "pulse", "save" }, Events);

        private void TheQueuedPulseShouldNotHaveRun() => Assert.IsFalse(Events.Contains("pulse"));

        private PulsePacket Pulse() => new()
        {
            Header = "pulse",
            KeepAliveId = (ushort)(Session.LastKeepAliveIdentity + 1),
            Tick = 60,
        };

        private sealed class RecordingPulseHandler(List<string> events) : PacketHandler<PulsePacket>, IWorldPacketHandler
        {
            public bool Throws { get; set; }

            public override Task ExecuteAsync(PulsePacket packet, ClientSession clientSession)
            {
                if (Throws)
                {
                    throw new InvalidOperationException("pulse failed");
                }

                events.Add("pulse");
                return Task.CompletedTask;
            }
        }
    }
}

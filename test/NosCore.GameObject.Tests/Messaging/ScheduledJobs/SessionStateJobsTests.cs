//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Core.Concurrency;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.ScheduledJobs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.ScheduledJobs
{
    [TestClass]
    public class SessionStateJobsTests
    {
        private ClientSession Session = null!;
        private Mock<ISaveService> SaveService = null!;
        private Mock<ISessionRegistry> SessionRegistry = null!;
        private FakeClock Clock = null!;
        private ClientSession.PacketLockScope? HeldLock;
        private Task Job = Task.CompletedTask;
        private int Saves;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Clock = new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0));
            SaveService = new Mock<ISaveService>();
            SaveService.Setup(s => s.SaveAsync(It.IsAny<ClientSession>()))
                .Callback(() => Saves++)
                .Returns(Task.CompletedTask);
            SessionRegistry = new Mock<ISessionRegistry>();
            SessionRegistry.Setup(s => s.GetSessions(It.IsAny<Func<ClientSession, bool>?>()))
                .Returns(() => new[] { Session });
            SessionRegistry.Setup(s => s.GetCharacters(It.IsAny<Func<PlayerComponentBundle, bool>?>()))
                .Returns(() => new[] { Session.Character });
        }

        [TestMethod]
        public async Task PeriodicSaveWaitsForThePacketInProgress()
        {
            await new Spec("Periodic save waits for the packet in progress")
                .GivenAsync(APacketIsBeingHandled)
                .When(ThePeriodicSaveRuns)
                .Then(NothingShouldBeSavedYet)
                .WhenAsync(ThePacketFinishes)
                .Then(TheCharacterShouldBeSavedOnce)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DisconnectSaveWaitsForThePacketInProgress()
        {
            await new Spec("Disconnect save waits for the packet in progress")
                .GivenAsync(APacketIsBeingHandled)
                .When(TheSessionDisconnects)
                .Then(NothingShouldBeSavedYet)
                .WhenAsync(ThePacketFinishes)
                .Then(TheCharacterShouldBeSavedOnce)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExpiredBonusesAreRemovedAfterThePacketInProgress()
        {
            await new Spec("Expired bonuses are removed after the packet in progress")
                .Given(TheCharacterHasAnExpiredBonus)
                .AndAsync(APacketIsBeingHandled)
                .When(TheExpiredBonusCleanupRuns)
                .Then(TheBonusShouldStillBeThere)
                .WhenAsync(ThePacketFinishes)
                .Then(TheBonusShouldBeRemoved)
                .ExecuteAsync();
        }

        private async Task APacketIsBeingHandled()
        {
            HeldLock = await Session.AcquirePacketLockAsync();
        }

        private async Task ThePacketFinishes()
        {
            HeldLock?.Dispose();
            HeldLock = null;
            await Job.WaitAsync(TimeSpan.FromSeconds(5));
        }

        private void ThePeriodicSaveRuns()
        {
            var handler = new SaveAllSessionsHandler(NullLogger<SaveAllSessionsHandler>.Instance, SaveService.Object,
                TestHelpers.Instance.LogLanguageLocalizer, SessionRegistry.Object);
            Job = handler.Handle(new SaveAllSessionsMessage());
        }

        private void TheSessionDisconnects()
        {
            Job = new SaveDisconnectHandler(SaveService.Object).HandleDisconnectAsync(Session);
        }

        private void TheCharacterHasAnExpiredBonus()
        {
            Session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                StaticBonusType = StaticBonusType.BackPack,
                DateEnd = Clock.GetCurrentInstant().Minus(Duration.FromMinutes(1))
            });
        }

        private void TheExpiredBonusCleanupRuns()
        {
            Job = new RemoveTimeoutStaticBonusesHandler(Clock, SessionRegistry.Object)
                .Handle(new RemoveTimeoutStaticBonusesMessage());
        }

        private void NothingShouldBeSavedYet()
        {
            Assert.AreEqual(0, Saves);
        }

        private void TheCharacterShouldBeSavedOnce()
        {
            Assert.AreEqual(1, Saves);
        }

        private void TheBonusShouldStillBeThere()
        {
            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
        }

        private void TheBonusShouldBeRemoved()
        {
            Assert.AreEqual(0, Session.Character.StaticBonusList.Count);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Event;
using NosCore.GameObject.Services.EventService;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.EventService
{
    [TestClass]
    public class EventScheduleTests
    {
        private static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);
        private ScheduledEventConfiguration _event = new();
        private LocalDateTime? _countdown;

        [TestMethod]
        public void AnIntervalCountsFromMidnight()
        {
            new Spec("An event every two hours has its countdown five minutes before the next even hour")
                .Given(AnEventEvery_HoursShiftedBy_Minutes, 2, 0)
                .When(LookingForTheNextCountdownAt_, new LocalDateTime(2026, 11, 13, 13, 0))
                .Then(TheCountdownShouldStartAt_, new LocalDateTime(2026, 11, 13, 13, 55))
                .Execute();
        }

        [TestMethod]
        public void TheLastSlotOfTheDayRollsOverToTomorrow()
        {
            new Spec("After the last slot of the day the next countdown is tomorrow's first")
                .Given(AnEventEvery_HoursShiftedBy_Minutes, 2, 0)
                .When(LookingForTheNextCountdownAt_, new LocalDateTime(2026, 11, 13, 23, 56))
                .Then(TheCountdownShouldStartAt_, new LocalDateTime(2026, 11, 14, 1, 55))
                .Execute();
        }

        [TestMethod]
        public void DailyTimesAreKeptAsGiven()
        {
            new Spec("An event listed at 14:00 and 19:00 counts down before whichever comes next")
                .Given(AnEventAt_And_, new TimeSpan(14, 0, 0), new TimeSpan(19, 0, 0))
                .When(LookingForTheNextCountdownAt_, new LocalDateTime(2026, 11, 13, 14, 30))
                .Then(TheCountdownShouldStartAt_, new LocalDateTime(2026, 11, 13, 18, 55))
                .Execute();
        }

        [TestMethod]
        public void AnEventKeptToSomeDaysWaitsForTheNextOfThem()
        {
            new Spec("A weekend event at 15:00 looked up on a Friday counts down on Saturday")
                .Given(AWeekendEventAt_, new TimeSpan(15, 0, 0))
                .When(LookingForTheNextCountdownAt_, new LocalDateTime(2026, 11, 13, 16, 0))
                .Then(TheCountdownShouldStartAt_, new LocalDateTime(2026, 11, 14, 14, 55))
                .Execute();
        }

        [TestMethod]
        public void AnEventKeptToSomeDaysRunsAgainTheNextDayItIsAllowed()
        {
            new Spec("A weekend event at 15:00 looked up after Saturday's start counts down on Sunday")
                .Given(AWeekendEventAt_, new TimeSpan(15, 0, 0))
                .When(LookingForTheNextCountdownAt_, new LocalDateTime(2026, 11, 14, 15, 30))
                .Then(TheCountdownShouldStartAt_, new LocalDateTime(2026, 11, 15, 14, 55))
                .Execute();
        }

        [TestMethod]
        public void AnEventKeptToSomeDaysIsFoundFromEarlyInTheWeek()
        {
            new Spec("A weekend event at 15:00 looked up on a Monday counts down on Saturday")
                .Given(AWeekendEventAt_, new TimeSpan(15, 0, 0))
                .When(LookingForTheNextCountdownAt_, new LocalDateTime(2026, 11, 16, 9, 0))
                .Then(TheCountdownShouldStartAt_, new LocalDateTime(2026, 11, 21, 14, 55))
                .Execute();
        }

        private void AWeekendEventAt_(TimeSpan at) =>
            _event = new ScheduledEventConfiguration
            {
                Type = EventType.MasterArena,
                Days = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                At = new List<TimeSpan> { at },
            };

        private void AnEventEvery_HoursShiftedBy_Minutes(int hours, int minutes) =>
            _event = new ScheduledEventConfiguration
            {
                Type = EventType.InstantCombat,
                Every = TimeSpan.FromHours(hours),
                Offset = TimeSpan.FromMinutes(minutes),
            };

        private void AnEventAt_And_(TimeSpan first, TimeSpan second) =>
            _event = new ScheduledEventConfiguration
            {
                Type = EventType.InstantCombat,
                At = new List<TimeSpan> { first, second },
            };

        private void LookingForTheNextCountdownAt_(LocalDateTime now) =>
            _countdown = EventSchedule.Start(_event, now, FiveMinutes);

        private void TheCountdownShouldStartAt_(LocalDateTime expected) => Assert.AreEqual(expected, _countdown);
    }
}

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

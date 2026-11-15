//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;

namespace NosCore.Tests.Shared
{
    /// <summary>
    /// The one clock a test world runs on: the instant services read and the timers and delays they
    /// wait on both move only when the test advances it.
    /// </summary>
    /// <remarks>
    /// Like a system timer, a periodic timer that a jump carries past several periods ticks once, so a
    /// test can move months ahead without replaying every life tick in between.
    /// </remarks>
    public sealed class TestClock(Instant start) : TimeProvider, IClock
    {
        private readonly Lock _sync = new();
        private readonly List<TestTimer> _timers = new();
        private DateTimeOffset _now = start.ToDateTimeOffset();

        public Instant GetCurrentInstant() => Instant.FromDateTimeOffset(GetUtcNow());

        public override DateTimeOffset GetUtcNow()
        {
            lock (_sync)
            {
                return _now;
            }
        }

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => GetUtcNow().UtcTicks;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new TestTimer(this, callback, state);
            timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(Duration duration) => Reset(GetCurrentInstant() + duration);

        public void AdvanceMilliseconds(long milliseconds) => Advance(Duration.FromMilliseconds(milliseconds));

        public void AdvanceSeconds(long seconds) => Advance(Duration.FromSeconds(seconds));

        public void AdvanceMinutes(long minutes) => Advance(Duration.FromMinutes(minutes));

        public void AdvanceHours(long hours) => Advance(Duration.FromHours(hours));

        public void AdvanceDays(int days) => Advance(Duration.FromDays(days));

        // Moves the clock to an instant and fires every timer that comes due on the way, earliest first.
        public void Reset(Instant instant)
        {
            var target = instant.ToDateTimeOffset();
            lock (_sync)
            {
                if (target < _now)
                {
                    throw new ArgumentOutOfRangeException(nameof(instant), "The clock never moves back.");
                }
            }

            while (true)
            {
                TestTimer? next;
                lock (_sync)
                {
                    next = _timers.Where(t => t.Due <= target).OrderBy(t => t.Due).FirstOrDefault();
                    _now = next?.Due is { } due && due > _now ? due : _now;
                    if (next == null)
                    {
                        _now = target;
                        return;
                    }

                    next.Reschedule(target);
                }

                next.Fire();
            }
        }

        private void Track(TestTimer timer, bool armed)
        {
            lock (_sync)
            {
                _timers.Remove(timer);
                if (armed)
                {
                    _timers.Add(timer);
                }
            }
        }

        private sealed class TestTimer(TestClock clock, TimerCallback callback, object? state) : ITimer
        {
            private TimeSpan _period = Timeout.InfiniteTimeSpan;

            public DateTimeOffset Due { get; private set; } = DateTimeOffset.MaxValue;

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                _period = period;
                var armed = dueTime != Timeout.InfiniteTimeSpan;
                Due = armed ? clock.GetUtcNow() + dueTime : DateTimeOffset.MaxValue;
                clock.Track(this, armed);
                return true;
            }

            // Called under the clock's lock just before the timer fires.
            public void Reschedule(DateTimeOffset target)
            {
                if (_period == Timeout.InfiniteTimeSpan || _period == TimeSpan.Zero)
                {
                    Due = DateTimeOffset.MaxValue;
                    clock._timers.Remove(this);
                    return;
                }

                var next = Due + _period;
                Due = next > target ? next : target + _period;
            }

            public void Fire() => callback(state);

            public void Dispose() => clock.Track(this, false);

            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.Core.Time
{
    /// <summary>
    /// The system clock plus an offset that only grows. Moving it forward brings every pending timer and
    /// delay forward with it, so a server can be taken past a schedule without waiting for it.
    /// </summary>
    public sealed class AdjustableTimeProvider : TimeProvider
    {
        private readonly Lock _sync = new();
        private readonly HashSet<ShiftedTimer> _timers = new();
        private TimeSpan _offset;

        public override DateTimeOffset GetUtcNow()
        {
            lock (_sync)
            {
                return System.GetUtcNow() + _offset;
            }
        }

        public override long GetTimestamp()
        {
            lock (_sync)
            {
                return System.GetTimestamp() + (long)(_offset.Ticks * ((double)TimestampFrequency / TimeSpan.TicksPerSecond));
            }
        }

        public void Advance(TimeSpan by)
        {
            if (by < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(by));
            }

            List<ShiftedTimer> timers;
            lock (_sync)
            {
                _offset += by;
                timers = new List<ShiftedTimer>(_timers);
            }

            foreach (var timer in timers)
            {
                timer.Reschedule();
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ShiftedTimer(this, callback, state);
            lock (_sync)
            {
                _timers.Add(timer);
            }

            timer.Change(dueTime, period);
            return timer;
        }

        private void Forget(ShiftedTimer timer)
        {
            lock (_sync)
            {
                _timers.Remove(timer);
            }
        }

        // Keeps its due time on the adjusted clock and arms a real timer for whatever is left of it.
        private sealed class ShiftedTimer : ITimer
        {
            private readonly AdjustableTimeProvider _owner;
            private readonly Timer _timer;
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private readonly Lock _sync = new();
            private DateTimeOffset? _due;
            private TimeSpan _period = Timeout.InfiniteTimeSpan;
            private bool _disposed;

            public ShiftedTimer(AdjustableTimeProvider owner, TimerCallback callback, object? state)
            {
                _owner = owner;
                _callback = callback;
                _state = state;
                _timer = new Timer(_ => Fire(), null, Timeout.Infinite, Timeout.Infinite);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (_sync)
                {
                    if (_disposed)
                    {
                        return false;
                    }

                    _period = period;
                    _due = dueTime == Timeout.InfiniteTimeSpan ? null : _owner.GetUtcNow() + dueTime;
                    Arm();
                    return true;
                }
            }

            public void Reschedule()
            {
                lock (_sync)
                {
                    if (!_disposed)
                    {
                        Arm();
                    }
                }
            }

            private void Arm()
            {
                if (_due is not { } due)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    return;
                }

                var left = due - _owner.GetUtcNow();
                _timer.Change(left > TimeSpan.Zero ? left : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            }

            private void Fire()
            {
                lock (_sync)
                {
                    if (_disposed || _due is not { } due)
                    {
                        return;
                    }

                    var now = _owner.GetUtcNow();
                    if (due > now)
                    {
                        Arm();
                        return;
                    }

                    // Like a system timer, periods skipped by a jump are not made up one by one.
                    _due = _period == Timeout.InfiniteTimeSpan || _period == TimeSpan.Zero ? null
                        : due + _period > now ? due + _period : now + _period;
                    Arm();
                }

                _callback(_state);
            }

            public void Dispose()
            {
                lock (_sync)
                {
                    _disposed = true;
                    _timer.Dispose();
                }

                _owner.Forget(this);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
        }
    }
}

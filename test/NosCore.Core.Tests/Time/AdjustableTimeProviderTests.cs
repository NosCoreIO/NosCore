//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Time;

namespace NosCore.Core.Tests.Time
{
    [TestClass]
    public class AdjustableTimeProviderTests
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

        private readonly AdjustableTimeProvider _time = new();

        [TestMethod]
        public void TheClockReadsAheadByWhatItWasMoved()
        {
            var before = _time.GetUtcNow();
            _time.Advance(TimeSpan.FromHours(2));
            var moved = _time.GetUtcNow() - before;
            Assert.IsTrue(moved >= TimeSpan.FromHours(2) && moved < TimeSpan.FromHours(2) + Patience);
        }

        [TestMethod]
        public void ElapsedTimeIncludesTheJump()
        {
            var started = _time.GetTimestamp();
            _time.Advance(TimeSpan.FromMinutes(20));
            Assert.IsTrue(_time.GetElapsedTime(started) >= TimeSpan.FromMinutes(20));
        }

        [TestMethod]
        public async Task ADelayEndsOnceTheClockPassesIt()
        {
            var delay = Task.Delay(TimeSpan.FromMinutes(20), _time);
            _time.Advance(TimeSpan.FromMinutes(19));
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            Assert.IsFalse(delay.IsCompleted);

            _time.Advance(TimeSpan.FromMinutes(1));
            await delay.WaitAsync(Patience);
        }

        [TestMethod]
        public async Task APeriodicTimerTicksOnceForAJumpOverManyPeriods()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), _time);
            _time.Advance(TimeSpan.FromMinutes(30));
            Assert.IsTrue(await timer.WaitForNextTickAsync().AsTask().WaitAsync(Patience));

            var next = timer.WaitForNextTickAsync().AsTask();
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            Assert.IsFalse(next.IsCompleted);
            _time.Advance(TimeSpan.FromMinutes(1));
            Assert.IsTrue(await next.WaitAsync(Patience));
        }

        [TestMethod]
        public void TheClockNeverMovesBack() =>
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _time.Advance(TimeSpan.FromSeconds(-1)));
    }
}

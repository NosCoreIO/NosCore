//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics.Metrics;
using System.Threading;
using NosCore.Core.Observability;

namespace NosCore.GameObject.Tests
{
    internal sealed class FailureCounter : IDisposable
    {
        private readonly MeterListener _listener = new();
        private int _count;

        public FailureCounter(string boundary)
        {
            _listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument == NosCoreMetrics.Failures)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "boundary" && Equals(tag.Value, boundary))
                    {
                        Interlocked.Add(ref _count, (int)value);
                    }
                }
            });
            _listener.Start();
        }

        public int Count => Volatile.Read(ref _count);

        public void Dispose() => _listener.Dispose();
    }
}

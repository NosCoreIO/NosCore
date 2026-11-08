//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    // Runs work against one map one item at a time, in arrival order: the life-loop tick,
    // hits, vitals and entering or leaving the map. Work that is already running on this
    // executor and asks for it again runs inline instead of queueing behind itself.
    //
    // Work must not wait on another executor's work or on a session's packet lock while it
    // runs here; a map change takes two executors at once through RunExclusiveAsync, in a
    // fixed order.
    public sealed class MapExecutor
    {
        private static readonly AsyncLocal<Scope?> Current = new();
        private static long _lastOrder;

        private readonly long _order = Interlocked.Increment(ref _lastOrder);
        private readonly SemaphoreSlim _gate = new(1, 1);
        private volatile bool _closed;

        public bool IsClosed => _closed;

        public bool IsCurrent
        {
            get
            {
                for (var scope = Current.Value; scope != null; scope = scope.Parent)
                {
                    if (!scope.Ended && ReferenceEquals(scope.Executor, this))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // Whether the current flow is running work on any map's executor.
        public static bool IsInsideAny
        {
            get
            {
                for (var scope = Current.Value; scope != null; scope = scope.Parent)
                {
                    if (!scope.Ended)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public async Task RunAsync(Func<Task> work)
        {
            await RunAsync(async () =>
            {
                await work();
                return true;
            });
        }

        public async Task<T> RunAsync<T>(Func<Task<T>> work)
        {
            if (IsCurrent)
            {
                return await work();
            }

            await _gate.WaitAsync();
            var scope = new Scope(this, Current.Value);
            try
            {
                ObjectDisposedException.ThrowIf(_closed, this);
                Current.Value = scope;
                return await work();
            }
            finally
            {
                scope.Ended = true;
                _gate.Release();
            }
        }

        // Holds two executors at once. They are always taken in the same order, so two flows
        // holding one each and waiting for the other cannot happen.
        public static async Task RunExclusiveAsync(MapExecutor first, MapExecutor second, Func<Task> work)
        {
            if (ReferenceEquals(first, second))
            {
                await first.RunAsync(work);
                return;
            }

            var (outer, inner) = first._order < second._order ? (first, second) : (second, first);
            if (inner.IsCurrent && !outer.IsCurrent)
            {
                throw new InvalidOperationException("A map executor cannot be taken while a later one is held");
            }

            await outer.RunAsync(() => inner.RunAsync(work));
        }

        // Refuses any later work; work already queued fails when its turn comes.
        public async Task CloseAsync()
        {
            if (IsCurrent)
            {
                _closed = true;
                return;
            }

            await _gate.WaitAsync();
            _closed = true;
            _gate.Release();
        }

        private sealed class Scope(MapExecutor executor, Scope? parent)
        {
            public MapExecutor Executor { get; } = executor;

            public Scope? Parent { get; } = parent;

            // Work started from inside the executor inherits the scope. Once the work that
            // took the executor has finished, such leftovers must queue like everyone else.
            public volatile bool Ended;
        }
    }
}

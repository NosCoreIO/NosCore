//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Observability;
using System;
using System.Diagnostics;
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
    // fixed order. A packet handler is the exception: it holds the map through StepAwayAsync
    // and gives it back while it waits on the master or moves the player to another map.
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
                    if (scope.Holds && ReferenceEquals(scope.Executor, this))
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
                    if (scope.Holds)
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

        public Task<T> RunAsync<T>(Func<Task<T>> work) => RunAsync(work, false);

        // Runs a packet handler: it holds this map like any other work, but may give it back
        // through StepAwayAsync while it waits on something outside the map.
        public async Task RunHandlerAsync(Func<Task> work)
        {
            if (IsInsideAny)
            {
                throw new InvalidOperationException("A packet handler cannot start inside a map's work");
            }

            await RunAsync(async () =>
            {
                await work();
                return true;
            }, true);
        }

        private async Task<T> RunAsync<T>(Func<Task<T>> work, bool handler)
        {
            if (IsCurrent)
            {
                return await work();
            }

            await AcquireAsync();
            var scope = new Scope(this, Current.Value, handler);
            try
            {
                ObjectDisposedException.ThrowIf(_closed, this);
                Current.Value = scope;
                return await work();
            }
            finally
            {
                scope.Ended = true;
                scope.Executor?._gate.Release();
            }
        }

        private async Task AcquireAsync()
        {
            var waitStarted = Stopwatch.GetTimestamp();
            await _gate.WaitAsync();
            NosCoreMetrics.MapExecutorWait.Record(Stopwatch.GetElapsedTime(waitStarted).TotalMilliseconds);
        }

        // Inside a packet handler, gives the map back while the work runs and then takes the
        // map that next names, which is where the player stands by then; no map when it
        // returns null. Anywhere else the work runs without letting go.
        public static async Task<T> StepAwayAsync<T>(Func<Task<T>> work, Func<MapExecutor?>? next = null)
        {
            if (HeldHandlerScope() is not { } scope)
            {
                return await work();
            }

            var held = scope.Executor;
            scope.Executor = null;
            held?._gate.Release();
            try
            {
                return await work();
            }
            finally
            {
                var target = next == null ? held : next();
                if (target != null)
                {
                    await target.AcquireAsync();
                }

                scope.Executor = target;
            }
        }

        public static async Task StepAwayAsync(Func<Task> work, Func<MapExecutor?>? next = null) =>
            await StepAwayAsync(async () =>
            {
                await work();
                return true;
            }, next);

        // Whether StepAwayAsync would give a map back here.
        public static bool CanStepAway => HeldHandlerScope() != null;

        private static Scope? HeldHandlerScope()
        {
            var scope = Current.Value;
            while (scope is { Ended: true })
            {
                scope = scope.Parent;
            }

            if (scope is not { Handler: true, Executor: not null })
            {
                return null;
            }

            for (var parent = scope.Parent; parent != null; parent = parent.Parent)
            {
                if (parent.Holds)
                {
                    return null;
                }
            }

            return scope;
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

        private sealed class Scope(MapExecutor executor, Scope? parent, bool handler)
        {
            // Null while a packet handler has stepped away from its map; another map once it
            // comes back after moving the player.
            public volatile MapExecutor? Executor = executor;

            public Scope? Parent { get; } = parent;

            public bool Handler { get; } = handler;

            public bool Holds => !Ended && Executor != null;

            // Work started from inside the executor inherits the scope. Once the work that
            // took the executor has finished, such leftovers must queue like everyone else.
            public volatile bool Ended;
        }
    }
}

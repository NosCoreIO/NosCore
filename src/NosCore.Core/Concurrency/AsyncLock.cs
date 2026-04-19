//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.Core.Concurrency
{
    // Thin async-disposable wrapper around SemaphoreSlim(1, 1). Use with `await using`
    // so the lock is always released, even on exception. Replaces the manual
    // WaitAsync()/Release() pattern that requires a try/finally at every call site.
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async ValueTask<Releaser> AcquireAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Releaser(_semaphore);
        }

        public void Dispose() => _semaphore.Dispose();

        public readonly struct Releaser(SemaphoreSlim semaphore) : IDisposable
        {
            public void Dispose() => semaphore.Release();
        }
    }
}

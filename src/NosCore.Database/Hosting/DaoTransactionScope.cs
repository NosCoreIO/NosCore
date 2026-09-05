//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NosCore.Core.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.Database.Hosting
{
    // Carries the transaction's DbContext to every DAO call on the same async flow:
    // the DbContext registration consults this slot before building a fresh context.
    // AsyncLocal keeps concurrent saves (Task.WhenAll over sessions) isolated from
    // each other.
    internal static class AmbientDbContext
    {
        private static readonly AsyncLocal<DbContext?> Slot = new();

        public static DbContext? Current => Slot.Value;

        public static void Set(DbContext? context)
        {
            Slot.Value = context;
        }
    }

    public sealed class DaoTransactionScope(Func<NosCoreContext> contextFactory) : IDaoTransactionScope
    {
        // Synchronous on purpose: an AsyncLocal written inside an awaited method does
        // not flow back to the caller, so the ambient slot must be set before the
        // first await of the calling flow.
        public IDaoTransaction Begin()
        {
            var context = contextFactory();
            var transaction = context.Database.BeginTransaction();
            AmbientDbContext.Set(context);
            return new DaoTransaction(context, transaction);
        }

        private sealed class DaoTransaction(NosCoreContext context, IDbContextTransaction transaction) : IDaoTransaction
        {
            public Task CommitAsync()
            {
                return transaction.CommitAsync();
            }

            public async ValueTask DisposeAsync()
            {
                AmbientDbContext.Set(null);
                await transaction.DisposeAsync();
                await context.DisposeAsync();
            }
        }
    }
}

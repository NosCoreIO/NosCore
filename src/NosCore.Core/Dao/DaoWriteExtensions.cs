//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;

namespace NosCore.Core.Dao
{
    // The DAO logs a failed write with its cause and reports it only through its result. These
    // turn that result into an exception for a caller that cannot carry on without the row.
    public static class DaoWriteExtensions
    {
        public static async Task<T> OrThrowAsync<T>(this Task<T> write) =>
            await write ?? throw new DaoWriteFailedException();

        public static async Task OrThrowAsync(this Task<bool> write)
        {
            if (!await write)
            {
                throw new DaoWriteFailedException();
            }
        }
    }

    public sealed class DaoWriteFailedException() : Exception("A database write failed; the DAO logged its cause.");
}

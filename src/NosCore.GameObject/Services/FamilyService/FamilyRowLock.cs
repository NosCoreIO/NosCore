//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Concurrency;

namespace NosCore.GameObject.Services.FamilyService
{
    // The master changes family rows from more than one service; each re-reads the row under this
    // lock so none writes back a copy older than another's change.
    internal static class FamilyRowLock
    {
        public static readonly AsyncLock Instance = new();
    }
}

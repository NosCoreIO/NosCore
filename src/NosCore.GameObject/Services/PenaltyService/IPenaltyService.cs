//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;

namespace NosCore.GameObject.Services.PenaltyService
{
    public interface IPenaltyService
    {
        // What is left of the account's longest running mute, or null when it may speak.
        Task<Duration?> GetMuteLeftAsync(long accountId);

        Task<bool> IsBannedAsync(long accountId);
    }
}

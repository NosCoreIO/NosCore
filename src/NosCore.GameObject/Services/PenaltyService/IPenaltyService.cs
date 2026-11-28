//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;

namespace NosCore.GameObject.Services.PenaltyService
{
    public interface IPenaltyService
    {
        // What is left of the account's longest running mute, or null when it may speak.
        Task<Duration?> GetMuteLeftAsync(long accountId);

        Task<bool> IsBannedAsync(long accountId);

        Task<bool> IsRunningAsync(long accountId, PenaltyType penalty);

        Task<bool> IsRunningForCharacterAsync(long characterId, PenaltyType penalty);

        // The account behind an active character of that server, or null when there is none.
        Task<AccountDto?> FindAccountAsync(string? characterName, int serverId);

        // A moderator only punishes accounts below their own authority, and never their own.
        bool MayPunish(AccountDto moderator, AccountDto target);

        Task GiveAsync(long accountId, PenaltyType penalty, Instant end, string reason, string adminName);

        // Ends every running penalty of that type now; false when none was running.
        Task<bool> LiftAsync(long accountId, PenaltyType penalty);
    }
}

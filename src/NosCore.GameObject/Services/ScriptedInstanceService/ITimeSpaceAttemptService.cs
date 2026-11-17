//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface ITimeSpaceAttemptService
    {
        // The failures counted so far, or null when they could not be saved.
        Task<short?> RecordFailureAsync(long characterId, short timeSpaceId);

        Task ResetAsync(long characterId, short timeSpaceId);

        Task<bool> OffersLoserModeAsync(long characterId, short timeSpaceId);
    }
}

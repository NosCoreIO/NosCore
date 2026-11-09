//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;

public static class BlacklistHubExtensions
{
    public static async Task<bool> IsBlacklistedEitherWayAsync(this IBlacklistHub blacklistHub, long characterId,
        long otherCharacterId)
    {
        var blacklisted = await blacklistHub.GetBlacklistedAsync(characterId);
        if (blacklisted?.Any(s => s.CharacterId == otherCharacterId) == true)
        {
            return true;
        }

        var blacklistedBy = await blacklistHub.GetBlacklistedAsync(otherCharacterId);
        return blacklistedBy?.Any(s => s.CharacterId == characterId) == true;
    }
}

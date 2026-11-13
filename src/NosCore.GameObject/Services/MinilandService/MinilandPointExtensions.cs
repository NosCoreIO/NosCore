//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Dto;

namespace NosCore.GameObject.Services.MinilandService
{
    public static class MinilandPointExtensions
    {
        public const long DailyMinilandPoints = 2000;

        public const long MinigameRewardCost = 100;

        public static Instant LastDailyReset(Instant now)
        {
            return now.InUtc().Date.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
        }

        // Points are not topped up: whatever is left at the reset is replaced by the daily allowance.
        public static bool RefreshDailyPoints(this MinilandDto miniland, Instant now)
        {
            if (miniland.MinilandPointRefreshedAt >= LastDailyReset(now))
            {
                return false;
            }

            miniland.MinilandPoint = DailyMinilandPoints;
            miniland.DailyVisitCount = 0;
            miniland.MinilandPointRefreshedAt = now;
            return true;
        }

        public static bool CanPayMinigameReward(this MinilandDto miniland)
        {
            return miniland.MinilandPoint >= MinigameRewardCost;
        }
    }
}

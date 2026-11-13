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

        public const int ReputationPerVisit = 2;

        public static int MinigameDurabilityCost(byte rewardLevel) => 10 + 5 * rewardLevel;

        public static Instant LastDailyReset(Instant now)
        {
            return now.InUtc().Date.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
        }

        // Points are not topped up: whatever is left at the reset is replaced by the daily allowance.
        // Returns the reputation the closed day's visits earned the owner, or null when no reset was due.
        public static long? RefreshDailyPoints(this MinilandDto miniland, Instant now)
        {
            if (miniland.MinilandPointRefreshedAt >= LastDailyReset(now))
            {
                return null;
            }

            var earned = (long)miniland.DailyVisitCount * ReputationPerVisit;
            miniland.MinilandPoint = DailyMinilandPoints;
            miniland.DailyVisitCount = 0;
            miniland.MinilandPointRefreshedAt = now;
            return earned;
        }

        public static bool CanPayMinigameReward(this MinilandDto miniland)
        {
            return miniland.MinilandPoint >= MinigameRewardCost;
        }
    }
}

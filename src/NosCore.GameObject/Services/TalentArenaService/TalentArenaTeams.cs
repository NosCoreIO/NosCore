//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.TalentArenaService
{
    public sealed record TalentArenaEntrant(long CharacterId, byte Level);

    // Teams of three whose levels lie within MaxLevelGap of each other, then two teams whose average
    // levels lie within the same gap.
    public static class TalentArenaTeams
    {
        public const int TeamSize = 3;
        public const int MaxLevelGap = 5;

        public static bool CanTeamUp(IReadOnlyCollection<TalentArenaEntrant> team, IReadOnlyCollection<TalentArenaEntrant> other)
        {
            if (team.Count + other.Count > TeamSize)
            {
                return false;
            }

            var levels = team.Concat(other).Select(e => (int)e.Level).ToList();
            return levels.Max() - levels.Min() <= MaxLevelGap;
        }

        public static bool AreRivals(IReadOnlyCollection<TalentArenaEntrant> team, IReadOnlyCollection<TalentArenaEntrant> other) =>
            team.Count == TeamSize && other.Count == TeamSize
            && System.Math.Abs(team.Average(e => e.Level) - other.Average(e => e.Level)) <= MaxLevelGap;
    }
}

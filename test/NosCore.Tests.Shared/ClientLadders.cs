//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Tests.Shared
{
    // Stands in for the rows ReputationLevelParser and DignityLevelParser import, so tests that
    // generate packets see the ladders a parsed database would hold.
    public static class ClientLadders
    {
        private static readonly long[] ReputationCeilings =
        [
            50, 150, 250, 500, 750, 1_000, 2_250, 3_500, 5_000, 9_500, 19_000, 25_000, 40_000,
            60_000, 85_000, 115_000, 150_000, 190_000, 235_000, 285_000, 350_000, 500_000,
            1_500_000, 2_500_000, 3_750_000, 5_000_000
        ];

        private static readonly short[] DignityCeilings = [-100, -201, -401, -601, -801];

        public static List<ReputationLevelDto> ReputationLevels()
        {
            var levels = new List<ReputationLevelDto>();
            long min = 0;
            foreach (var ceiling in ReputationCeilings)
            {
                levels.Add(new ReputationLevelDto
                {
                    ReputationLevelId = (byte)(levels.Count + 1),
                    MinReputation = min,
                    MaxReputation = ceiling
                });
                min = ceiling + 1;
            }

            levels.Add(new ReputationLevelDto
            {
                ReputationLevelId = (byte)ReputationType.RedElite,
                MinReputation = min,
                MaxReputation = null
            });

            return levels;
        }

        public static List<DignityLevelDto> DignityLevels()
        {
            var levels = new List<DignityLevelDto>
            {
                new() { DignityLevelId = (byte)DignityType.Default, MaxDignity = null }
            };

            levels.AddRange(DignityCeilings.Select((ceiling, index) => new DignityLevelDto
            {
                DignityLevelId = (byte)(index + 2),
                MaxDignity = ceiling
            }));

            return levels;
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs;

/// <summary>
/// The reputation icon a given amount of reputation earns.
/// </summary>
/// <remarks>
/// Twenty-seven bands, matching the enum's first twenty-seven values one for one. Above them the
/// ladder is by rank rather than by amount, which needs a ranking this project does not keep, and
/// ReputationType has no RedLegend to name the tier with.
/// </remarks>
public static class ReputationLevels
{
    public static ReputationType FromReputation(long reputation) => reputation switch
    {
        >= 5_000_001 => ReputationType.RedElite,          // "Over 5000000"
        >= 3_750_001 => ReputationType.BlueElite,         // 3750001 - 5000000
        >= 2_500_001 => ReputationType.GreenElite,        // 2500001 - 3750000
        >= 1_500_001 => ReputationType.RedNos,            // 1500001 - 2500000
        >= 500_001 => ReputationType.BlueNos,             // 500001 - 1500000
        >= 350_001 => ReputationType.GreenNos,            // 350001 - 500000
        >= 285_001 => ReputationType.RedMaster,           // 285001 - 350000
        >= 235_001 => ReputationType.BlueMaster,          // 235001 - 285000
        >= 190_001 => ReputationType.GreenMaster,         // 190001 - 235000
        >= 150_001 => ReputationType.RedLeader,           // 150001 - 190000
        >= 115_001 => ReputationType.BlueLeader,          // 115001 - 150000
        >= 85_001 => ReputationType.GreenLeader,          // 85001 - 115000
        >= 60_001 => ReputationType.RedExpert,            // 60001 - 85000
        >= 40_001 => ReputationType.BlueExpert,           // 40001 - 60000
        >= 25_001 => ReputationType.GreenExpert,          // 25001 - 40000
        >= 19_001 => ReputationType.RedSoldier,           // 19001 - 25000
        >= 9_501 => ReputationType.BlueSoldier,           // 9501 - 19000
        >= 5_001 => ReputationType.GreenSoldier,          // 5001 - 9500
        >= 3_501 => ReputationType.RedExperienced,        // 3501 - 5000
        >= 2_251 => ReputationType.BlueExperienced,       // 2251 - 3500
        >= 1_001 => ReputationType.GreenExperienced,      // 1001 - 2250
        >= 751 => ReputationType.RedTrainee,              // 751 - 1000
        >= 501 => ReputationType.BlueTrainee,             // 501 - 750
        >= 251 => ReputationType.GreenTrainee,            // 251 - 500
        >= 151 => ReputationType.RedBeginner,             // 151 - 250
        >= 51 => ReputationType.BlueBeginner,             // 51 - 150
        _ => ReputationType.GreenBeginner                 // 0 - 50
    };
}

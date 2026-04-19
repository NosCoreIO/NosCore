//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs;

public static class ReputationLevels
{
    public static ReputationType FromReputation(long reputation) => reputation switch
    {
        >= 5_000_001 => ReputationType.RedElite,
        >= 4_950_001 => ReputationType.BlueElite,
        >= 2_500_001 => ReputationType.GreenElite,
        >= 2_450_001 => ReputationType.RedNos,
        >= 500_001 => ReputationType.BlueNos,
        >= 495_001 => ReputationType.GreenNos,
        >= 245_001 => ReputationType.BlueMaster,
        >= 95_001 => ReputationType.GreenLeader,
        >= 45_001 => ReputationType.BlueExpert,
        >= 25_001 => ReputationType.GreenExpert,
        >= 19_001 => ReputationType.RedSoldier,
        >= 9_501 => ReputationType.BlueSoldier,
        >= 5_001 => ReputationType.GreenSoldier,
        >= 4_901 => ReputationType.RedExperienced,
        >= 2_251 => ReputationType.BlueExperienced,
        >= 2_001 => ReputationType.GreenExperienced,
        >= 501 => ReputationType.BlueTrainee,
        >= 251 => ReputationType.GreenTrainee,
        >= 201 => ReputationType.RedBeginner,
        _ => ReputationType.GreenBeginner
    };
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs;

// The ladder is imported from the client by ReputationLevelParser into ReputationLevel rows.
// ReputIcon is read from PlayerComponentBundle, a readonly struct the ECS builds without the
// container, so the table is published here once at startup rather than injected. Servers
// running against a database parsed before the table existed keep working on ClientLadder,
// which holds the same 27 bands the parser produces.
public static class ReputationLevels
{
    private static readonly ReputationBand[] ClientLadder =
    [
        new(5_000_001, ReputationType.RedElite),
        new(3_750_001, ReputationType.BlueElite),
        new(2_500_001, ReputationType.GreenElite),
        new(1_500_001, ReputationType.RedNos),
        new(500_001, ReputationType.BlueNos),
        new(350_001, ReputationType.GreenNos),
        new(285_001, ReputationType.RedMaster),
        new(235_001, ReputationType.BlueMaster),
        new(190_001, ReputationType.GreenMaster),
        new(150_001, ReputationType.RedLeader),
        new(115_001, ReputationType.BlueLeader),
        new(85_001, ReputationType.GreenLeader),
        new(60_001, ReputationType.RedExpert),
        new(40_001, ReputationType.BlueExpert),
        new(25_001, ReputationType.GreenExpert),
        new(19_001, ReputationType.RedSoldier),
        new(9_501, ReputationType.BlueSoldier),
        new(5_001, ReputationType.GreenSoldier),
        new(3_501, ReputationType.RedExperienced),
        new(2_251, ReputationType.BlueExperienced),
        new(1_001, ReputationType.GreenExperienced),
        new(751, ReputationType.RedTrainee),
        new(501, ReputationType.BlueTrainee),
        new(251, ReputationType.GreenTrainee),
        new(151, ReputationType.RedBeginner),
        new(51, ReputationType.BlueBeginner),
        new(0, ReputationType.GreenBeginner)
    ];

    private static volatile ReputationBand[] _ladder = ClientLadder;

    public static void Load(IEnumerable<ReputationLevelDto> levels)
    {
        var ladder = levels
            .OrderByDescending(level => level.MinReputation)
            .Select(level => new ReputationBand(level.MinReputation, (ReputationType)level.ReputationLevelId))
            .ToArray();

        if (ladder.Length == 0)
        {
            return;
        }

        _ladder = ladder;
    }

    public static void ResetToClientLadder()
    {
        _ladder = ClientLadder;
    }

    public static ReputationType FromReputation(long reputation)
    {
        var ladder = _ladder;
        foreach (var band in ladder)
        {
            if (reputation >= band.MinReputation)
            {
                return band.Tier;
            }
        }

        return ladder[^1].Tier;
    }

    private readonly record struct ReputationBand(long MinReputation, ReputationType Tier);
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.CharacterRankingService;

public static class CharacterRankingsExtensions
{
    public static ClinitPacket GeneratePopularityList(this CharacterRankings rankings) => new()
    {
        SubPackets = rankings.Popularity.Select(c => (ClinitSubPacket?)new ClinitSubPacket
        {
            CharacterId = c.CharacterId,
            Level = c.Level,
            HeroLevel = c.HeroLevel,
            Compliment = (int)c.Score,
            CharacterName = c.Name
        }).ToList()
    };

    public static FlinitPacket GenerateReputationList(this CharacterRankings rankings) => new()
    {
        SubPackets = rankings.Reputation.Select(c => (FlinitSubPacket?)new FlinitSubPacket
        {
            CharacterId = c.CharacterId,
            Level = c.Level,
            HeroLevel = c.HeroLevel,
            Reputation = c.Score,
            CharacterName = c.Name
        }).ToList()
    };

    public static KdlinitPacket GenerateCombatPointsList(this CharacterRankings rankings) => new()
    {
        SubPackets = rankings.CombatPoints.Select(c => (KdlinitSubPacket?)new KdlinitSubPacket
        {
            CharacterId = c.CharacterId,
            Level = c.Level,
            HeroLevel = c.HeroLevel,
            Act4Points = (int)c.Score,
            CharacterName = c.Name
        }).ToList()
    };
}

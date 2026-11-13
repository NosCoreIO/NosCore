//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.GameObject.Services.CharacterRankingService;

public interface ICharacterRankingService
{
    CharacterRankings GetRankings();
}

public sealed record RankedCharacter(long CharacterId, byte Level, byte HeroLevel, long Score, string Name);

public sealed record CharacterRankings(
    IReadOnlyList<RankedCharacter> Popularity,
    IReadOnlyList<RankedCharacter> Reputation,
    IReadOnlyList<RankedCharacter> CombatPoints);

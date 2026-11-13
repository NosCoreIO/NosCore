//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Infastructure;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.CharacterRankingService;

// The Messenger's three ranking tabs. They hold the active player characters of this world
// server; staff and closed or banned accounts are left out. The client tells the player the
// lists change once a day at 5 AM, so they are built then and kept in between.
public class CharacterRankingService(IDao<CharacterDto, long> characterDao, IDao<AccountDto, long> accountDao,
        IOptions<WorldConfiguration> worldConfiguration, IClock clock)
    : ICharacterRankingService, ISingletonService
{
    public const int PopularityPlaces = 30;
    public const int ReputationPlaces = 43;
    public const int CombatPointsPlaces = 30;

    private static readonly LocalTime RefreshTime = new(5, 0);

    private readonly DateTimeZone _zone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
    private Built? _built;

    public CharacterRankings GetRankings()
    {
        var now = clock.GetCurrentInstant();
        var built = _built;
        if (built == null || built.At < LastRefresh(now))
        {
            _built = built = new Built(Build(), now);
        }

        return built.Rankings;
    }

    private Instant LastRefresh(Instant now)
    {
        var local = now.InZone(_zone).LocalDateTime;
        var day = local.TimeOfDay >= RefreshTime ? local.Date : local.Date.PlusDays(-1);
        return _zone.AtLeniently(day + RefreshTime).ToInstant();
    }

    private CharacterRankings Build()
    {
        var unranked = accountDao.Where(a => a.Authority != AuthorityType.User)?
            .Select(a => a.AccountId).ToList() ?? [];
        var serverId = worldConfiguration.Value.ServerId;
        var characters = characterDao.Where(c => c.ServerId == serverId && c.State == CharacterState.Active
                && !unranked.Contains(c.AccountId))?.ToList() ?? [];
        return new CharacterRankings(
            Top(characters, c => c.Compliment, PopularityPlaces),
            Top(characters, c => c.Reput, ReputationPlaces),
            Top(characters, c => c.Act4Points, CombatPointsPlaces));
    }

    // A character with nothing to show is not ranked: on a young server the lists would
    // otherwise be every character in the order they were created.
    private static IReadOnlyList<RankedCharacter> Top(IEnumerable<CharacterDto> characters,
        Func<CharacterDto, long> score, int places) =>
        characters.Select(c => (Character: c, Score: score(c)))
            .Where(c => c.Score > 0)
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Character.Name, StringComparer.Ordinal)
            .Take(places)
            .Select(c => new RankedCharacter(c.Character.CharacterId, c.Character.Level, c.Character.HeroLevel,
                c.Score, c.Character.Name))
            .ToList();

    private sealed record Built(CharacterRankings Rankings, Instant At);
}

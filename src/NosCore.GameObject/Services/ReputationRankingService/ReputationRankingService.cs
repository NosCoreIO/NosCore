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
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ReputationRankingService;

// The ranking holds the active player characters of this world server; staff and closed or
// banned accounts are left out of it.
public class ReputationRankingService(IDao<CharacterDto, long> characterDao, IDao<AccountDto, long> accountDao,
        IOptions<WorldConfiguration> worldConfiguration, IClock clock)
    : IReputationRankingService, ISingletonService
{
    private const int TopPlaces = 3;

    public static readonly Duration RefreshInterval = Duration.FromSeconds(30);

    private CachedLeaders? _leaders;

    public bool IsTopThree(long characterId, long reputation, AuthorityType authority)
    {
        if (authority != AuthorityType.User)
        {
            return false;
        }

        // The caller's live reputation is used rather than its last saved row, which is why
        // one more row than there are places is kept: the caller may be among them.
        var ahead = GetLeaders().Count(c => c.CharacterId != characterId && c.Reput > reputation);
        return ahead < TopPlaces;
    }

    private IReadOnlyList<RankedCharacter> GetLeaders()
    {
        var now = clock.GetCurrentInstant();
        var cached = _leaders;
        if (cached != null && cached.ExpiresAt > now)
        {
            return cached.Leaders;
        }

        var unranked = accountDao.Where(a => a.Authority != AuthorityType.User)?
            .Select(a => a.AccountId).ToList() ?? [];
        var serverId = worldConfiguration.Value.ServerId;
        var leaders = characterDao.Where(c => c.ServerId == serverId && c.State == CharacterState.Active
                && !unranked.Contains(c.AccountId))?
            .OrderByDescending(c => c.Reput)
            .Take(TopPlaces + 1)
            .Select(c => new RankedCharacter(c.CharacterId, c.Reput))
            .ToList() ?? [];

        _leaders = new CachedLeaders(leaders, now + RefreshInterval);
        return leaders;
    }

    private sealed record RankedCharacter(long CharacterId, long Reput);

    private sealed record CachedLeaders(IReadOnlyList<RankedCharacter> Leaders, Instant ExpiresAt);
}

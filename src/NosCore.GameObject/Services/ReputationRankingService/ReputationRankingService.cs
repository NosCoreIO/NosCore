//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Shared.Enumerations;
using System.Linq;

namespace NosCore.GameObject.Services.ReputationRankingService;

// The ranking holds the active player characters of this world server; staff and closed or
// banned accounts are left out of it.
public class ReputationRankingService(IDao<CharacterDto, long> characterDao, IDao<AccountDto, long> accountDao,
        IOptions<WorldConfiguration> worldConfiguration)
    : IReputationRankingService
{
    private const int TopPlaces = 3;

    public bool IsTopThree(long characterId, long reputation, AuthorityType authority)
    {
        if (authority != AuthorityType.User)
        {
            return false;
        }

        var unranked = accountDao.Where(a => a.Authority != AuthorityType.User)?
            .Select(a => a.AccountId).ToList() ?? [];
        var serverId = worldConfiguration.Value.ServerId;

        // The caller's live reputation is used rather than its last saved row.
        var ahead = characterDao.Where(c => c.ServerId == serverId && c.State == CharacterState.Active
            && c.CharacterId != characterId && c.Reput > reputation && !unranked.Contains(c.AccountId));

        return (ahead?.Take(TopPlaces).Count() ?? 0) < TopPlaces;
    }
}

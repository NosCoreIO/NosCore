//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ReputationRankingService;

public interface IReputationRankingService
{
    bool IsTopThree(long characterId, long reputation, AuthorityType authority);
}

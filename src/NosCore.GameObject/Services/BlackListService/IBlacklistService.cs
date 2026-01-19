//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BlackListService
{
    public interface IBlacklistService
    {
        Task<LanguageKey> BlacklistPlayerAsync(long characterId, long secondCharacterId);
        Task<List<CharacterRelationStatus>> GetBlacklistedListAsync(long id);
        Task<bool> UnblacklistAsync(Guid id);
    }
}

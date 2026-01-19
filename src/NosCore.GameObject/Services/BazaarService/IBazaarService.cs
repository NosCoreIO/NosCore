//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BazaarService
{
    public interface IBazaarService
    {
        List<BazaarLink> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter);

        Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName, long? requestCharacterId = null);

        Task<LanguageKey> AddBazaarAsync(Guid itemInstanceId, long characterId, string? characterName, bool hasMedal, long price, bool isPackage, short duration, short amount);

        Task<BazaarLink?> ModifyBazaarAsync(long id, Json.Patch.JsonPatch bzMod);
    }
}

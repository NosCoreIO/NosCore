//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.SignalR;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.BazaarService;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub
{
    public class BazaarHub(IBazaarService bazaarService) : Hub, IBazaarHub
    {
        public Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter) => Task.FromResult(bazaarService.GetBazaar(id, index, pageSize, typeFilter,
            subTypeFilter, levelFilter, rareFilter, upgradeFilter, sellerFilter));

        public Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName, long? requestCharacterId = null) => bazaarService.DeleteBazaarAsync(id, count, requestCharacterName, requestCharacterId);

        public Task<LanguageKey> AddBazaarAsync(BazaarRequest bazaarRequest) => bazaarService.AddBazaarAsync(bazaarRequest.ItemInstanceId,
            bazaarRequest.CharacterId, bazaarRequest.CharacterName, bazaarRequest.HasMedal, bazaarRequest.Price, bazaarRequest.IsPackage, bazaarRequest.Duration, bazaarRequest.Amount);

        public Task<BazaarLink?> ModifyBazaarAsync(long id, Json.Patch.JsonPatch bzMod) => bazaarService.ModifyBazaarAsync(id, bzMod);
    }
}

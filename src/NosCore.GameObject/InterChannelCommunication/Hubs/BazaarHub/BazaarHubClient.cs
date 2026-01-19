//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub
{
    public class BazaarHubClient(HubConnectionFactory hubConnectionFactory, ILogger logger)
        : BaseHubClient(hubConnectionFactory, nameof(BazaarHub), logger), IBazaarHub
    {
        public Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter) =>
            InvokeAsync<List<BazaarLink>>(nameof(GetBazaar), id, index, pageSize, typeFilter,
                subTypeFilter, levelFilter, rareFilter, upgradeFilter, sellerFilter);

        public Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName, long? requestCharacterId = null) =>
            InvokeAsync<bool>(nameof(DeleteBazaarAsync), id, count, requestCharacterName, requestCharacterId);

        public Task<LanguageKey> AddBazaarAsync(BazaarRequest bazaarRequest) =>
            InvokeAsync<LanguageKey>(nameof(AddBazaarAsync), bazaarRequest);

        public Task<BazaarLink?> ModifyBazaarAsync(long id, Json.Patch.JsonPatch bzMod) =>
            InvokeAsync<BazaarLink?>(nameof(ModifyBazaarAsync), id, bzMod);
    }
}

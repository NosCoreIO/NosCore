//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub
{
    public class BazaarHubClient(IMessageBus bus) : IBazaarHub
    {
        public Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter) =>
            bus.RequestAsync<List<BazaarLink>>(new GetBazaarRequest(id, index, pageSize, typeFilter,
                subTypeFilter, levelFilter, rareFilter, upgradeFilter, sellerFilter));

        public Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName, long? requestCharacterId = null) =>
            bus.RequestAsync<bool>(new DeleteBazaarRequest(id, count, requestCharacterName, requestCharacterId));

        public Task<LanguageKey> AddBazaarAsync(BazaarRequest bazaarRequest) =>
            bus.RequestAsync<LanguageKey>(new AddBazaarRequest(bazaarRequest));

        public Task<BazaarLink?> ModifyBazaarAsync(long id, Json.Patch.JsonPatch bzMod) =>
            bus.RequestAsync<BazaarLink?>(new ModifyBazaarRequest(id, bzMod));
    }
}

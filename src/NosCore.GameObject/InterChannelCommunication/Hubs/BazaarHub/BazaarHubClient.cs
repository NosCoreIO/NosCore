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
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub
{
    public class BazaarHubClient(IMessageBus bus) : IBazaarHub
    {
        public Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter) =>
            bus.RequestAsync<List<BazaarLink>>(new GetBazaarRequest(id, index, pageSize, typeFilter,
                subTypeFilter, levelFilter, rareFilter, upgradeFilter, sellerFilter));

        public Task<bool> BuyBazaarAsync(Guid operationId, long id, short count, long price, long buyerId) =>
            bus.RequestAsync<bool>(new BuyBazaarRequest(operationId, id, count, price, buyerId));

        public Task<BazaarClaim?> ClaimBazaarAsync(Guid operationId, long id, long sellerId) =>
            bus.RequestAsync<BazaarClaim?>(new ClaimBazaarRequest(operationId, id, sellerId));

        public Task<bool> SettleBazaarAsync(Guid operationId) =>
            bus.RequestAsync<bool>(new SettleBazaarRequest(operationId));

        public Task<bool> CompensateBazaarAsync(Guid operationId) =>
            bus.RequestAsync<bool>(new CompensateBazaarRequest(operationId));

        public Task<LanguageKey> AddBazaarAsync(BazaarRequest bazaarRequest) =>
            bus.RequestAsync<LanguageKey>(new AddBazaarRequest(bazaarRequest));

        public Task<BazaarLink?> ModifyBazaarAsync(Guid operationId, long id, long sellerId, long price) =>
            bus.RequestAsync<BazaarLink?>(new ModifyBazaarRequest(operationId, id, sellerId, price));
    }
}

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

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;

public interface IBazaarHub
{
    Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
        byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter);
    Task<List<BazaarLink>> SearchBazaarAsync(BazaarSearch search);
    Task<LanguageKey> AddBazaarAsync(BazaarRequest bazaarRequest);
    Task<bool> BuyBazaarAsync(Guid operationId, long id, short count, long price, long buyerId);
    Task<BazaarLink?> ModifyBazaarAsync(Guid operationId, long id, long sellerId, long price);
    Task<BazaarClaim?> ClaimBazaarAsync(Guid operationId, long id, long sellerId);
    Task<bool> SettleBazaarAsync(Guid operationId);
    Task<bool> CompensateBazaarAsync(Guid operationId);
}

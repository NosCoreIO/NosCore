//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.BazaarService;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub
{
    public class BazaarHub(IBazaarService bazaarService) : IBazaarHub
    {
        public Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter) => Task.FromResult(bazaarService.GetBazaar(id, index, pageSize, typeFilter,
            subTypeFilter, levelFilter, rareFilter, upgradeFilter, sellerFilter));

        public Task<bool> BuyBazaarAsync(Guid operationId, long id, short count, long price, long buyerId) =>
            bazaarService.BuyBazaarAsync(operationId, id, count, price, buyerId);

        public Task<List<BazaarLink>> SearchBazaarAsync(BazaarSearch search) => Task.FromResult(bazaarService.SearchBazaar(search));

        public Task<BazaarClaim?> ClaimBazaarAsync(Guid operationId, long id, long sellerId) =>
            bazaarService.ClaimBazaarAsync(operationId, id, sellerId);

        public Task<bool> SettleBazaarAsync(Guid operationId) => bazaarService.SettleBazaarAsync(operationId);

        public Task<bool> CompensateBazaarAsync(Guid operationId) => bazaarService.CompensateBazaarAsync(operationId);

        public Task<LanguageKey> AddBazaarAsync(BazaarRequest bazaarRequest) => bazaarService.AddBazaarAsync(bazaarRequest.OperationId, bazaarRequest.ItemInstanceId,
            bazaarRequest.CharacterId, bazaarRequest.CharacterName, bazaarRequest.HasMedal, bazaarRequest.Price, bazaarRequest.IsPackage, bazaarRequest.Duration, bazaarRequest.Amount);

        public Task<BazaarLink?> ModifyBazaarAsync(Guid operationId, long id, long sellerId, long price) =>
            bazaarService.ModifyBazaarAsync(operationId, id, sellerId, price);

        public static async Task<Reply<List<BazaarLink>>> Handle(GetBazaarRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<BazaarHub>().GetBazaar(r.Id, r.Index, r.PageSize, r.TypeFilter, r.SubTypeFilter, r.LevelFilter, r.RareFilter, r.UpgradeFilter, r.SellerFilter));

        public static async Task<Reply<bool>> Handle(BuyBazaarRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<BazaarHub>().BuyBazaarAsync(r.OperationId, r.Id, r.Count, r.Price, r.BuyerId));

        public static async Task<Reply<BazaarClaim?>> Handle(ClaimBazaarRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<BazaarHub>().ClaimBazaarAsync(r.OperationId, r.Id, r.SellerId));

        public static async Task<Reply<bool>> Handle(SettleBazaarRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<BazaarHub>().SettleBazaarAsync(r.OperationId));

        public static async Task<Reply<bool>> Handle(CompensateBazaarRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<BazaarHub>().CompensateBazaarAsync(r.OperationId));

        public static async Task<Reply<List<BazaarLink>>> Handle(SearchBazaarRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<BazaarHub>().SearchBazaarAsync(r.Search));

        public static async Task<Reply<LanguageKey>> Handle(AddBazaarRequest r, IServiceProvider services) => new(await services.GetRequiredService<BazaarHub>().AddBazaarAsync(r.BazaarRequest));

        public static async Task<Reply<BazaarLink?>> Handle(ModifyBazaarRequest r, IServiceProvider services) => new(await services.GetRequiredService<BazaarHub>().ModifyBazaarAsync(r.OperationId, r.Id, r.SellerId, r.Price));
    }
}

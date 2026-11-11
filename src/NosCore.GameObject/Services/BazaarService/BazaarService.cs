//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NodaTime;
using NosCore.Core.Concurrency;
using System.Collections.Concurrent;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BazaarService
{
    // The master is the only party that sees every channel, so every bazaar change happens
    // here, under the listing's lock, in one transaction with its operation record. Each
    // operation carries an id chosen by the channel: a retried id replays its first result.
    // Register, buy and claim stay pending until the channel applied its side and settles
    // them, or could not and compensates them; a claim never pays for a pending buy.
    public class BazaarService(IBazaarRegistry bazaarRegistry, IDao<BazaarItemDto, long> bazaarItemDao,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, IDao<BazaarOperationDto, Guid> bazaarOperationDao,
            IDaoTransactionScope transactionScope, IClock clock, List<ItemDto> items)
        : IBazaarService
    {
        private static readonly ConcurrentDictionary<long, AsyncLock> ListingLocks = new();
        private static readonly ConcurrentDictionary<long, AsyncLock> SellerLocks = new();

        // Every operation takes its id's lock before the listing's, so a request and its
        // compensation never both decide the id is new.
        private static readonly AsyncLock[] OperationLocks = Enumerable.Range(0, 64).Select(_ => new AsyncLock()).ToArray();

        private static AsyncLock OperationLock(Guid id) =>
            OperationLocks[(id.GetHashCode() & int.MaxValue) % OperationLocks.Length];

        private static AsyncLock ListingLock(long id) => ListingLocks.GetOrAdd(id, _ => new AsyncLock());

        private static AsyncLock SellerLock(long id) => SellerLocks.GetOrAdd(id, _ => new AsyncLock());

        public List<BazaarLink> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter)
        {
            IEnumerable<BazaarLink> bzlinks;
            if (id != -1)
            {
                var item = bazaarRegistry.GetById(id);
                bzlinks = item != null ? new[] { item } : Enumerable.Empty<BazaarLink>();
            }
            else
            {
                bzlinks = sellerFilter != null
                    ? bazaarRegistry.GetBySellerId(sellerFilter.Value)
                    : bazaarRegistry.GetAll().Where(s => !bazaarRegistry.IsHidden(s.BazaarItem?.BazaarItemId ?? 0));
            }

            if (typeFilter is { } type && type != BazaarListType.Default)
            {
                var links = bzlinks.Where(s => s.ItemInstance != null).ToList();
                var vnums = links.Select(s => s.ItemInstance.ItemVNum)
                    .Concat(links.Select(s => (s.ItemInstance as BoxInstanceDto)?.HoldingVNum ?? 0))
                    .ToHashSet();
                var itemsByVNum = items.Where(s => vnums.Contains(s.VNum)).ToDictionary(s => s.VNum);
                var filter = new BazaarFilter(type, subTypeFilter ?? 0, levelFilter ?? 0, rareFilter ?? 0, upgradeFilter ?? 0);
                bzlinks = links.Where(s => itemsByVNum.TryGetValue(s.ItemInstance.ItemVNum, out var item)
                    && filter.Matches(s.ItemInstance, item, itemsByVNum));
            }

            if (pageSize is { } size)
            {
                bzlinks = bzlinks.Skip((index ?? 0) * size).Take(size);
            }

            return bzlinks.ToList();
        }

        public List<BazaarLink> SearchBazaar(BazaarSearch search)
        {
            var vnumFilter = search.ItemVNumFilter.Where(s => s != 0).ToHashSet();
            var now = clock.GetCurrentInstant();
            var searched = GetBazaar(-1, null, null, search.TypeFilter, search.SubTypeFilter, search.LevelFilter,
                    search.RareFilter, search.UpgradeFilter, null)
                .Where(s => (vnumFilter.Count == 0 || vnumFilter.Contains(s.ItemInstance.ItemVNum))
                    && s.BazaarItem.DateStart.Plus(Duration.FromHours(s.BazaarItem.Duration)) > now
                    && s.ItemInstance.Amount > 0)
                .ToList();

            var vnums = searched.Select(s => s.ItemInstance.ItemVNum).ToHashSet();
            var names = items.Where(s => vnums.Contains(s.VNum))
                .ToDictionary(s => s.VNum, s => s.Name[search.Language]);
            var byName = searched.OrderBy(s => names.GetValueOrDefault(s.ItemInstance.ItemVNum) ?? "");
            var ordered = search.OrderFilter switch
            {
                0 => byName.ThenBy(s => s.BazaarItem.Price),
                1 => byName.ThenByDescending(s => s.BazaarItem.Price),
                2 => byName.ThenBy(s => s.BazaarItem.Amount),
                3 => byName.ThenByDescending(s => s.BazaarItem.Amount),
                _ => byName
            };

            return ordered.Skip(search.Index * search.PageSize).Take(search.PageSize).ToList();
        }

        public async Task<LanguageKey> AddBazaarAsync(Guid operationId, Guid itemInstanceId, long characterId,
            string? characterName, bool hasMedal, long price, bool isPackage, short duration, short amount)
        {
            using var operationClaim = await OperationLock(operationId).AcquireAsync();
            using var sellerClaim = await SellerLock(characterId).AcquireAsync();
            var replay = await FindOperationAsync(operationId);
            if (replay != null)
            {
                return replay.Type == BazaarOperationType.Register && replay.State != BazaarOperationState.Compensated
                    ? LanguageKey.OBJECT_IN_BAZAAR
                    : throw new InvalidOperationException();
            }

            if (bazaarRegistry.CountBySellerId(characterId) > 10 * (hasMedal ? 10 : 1) - 1)
            {
                return LanguageKey.LIMIT_EXCEEDED;
            }

            var item = await itemInstanceDao.FirstOrDefaultAsync(s => s.Id == itemInstanceId);
            if ((item == null) || (item.Amount != amount) || (amount <= 0) || (price < 0))
            {
                throw new ArgumentException();
            }

            BazaarItemDto? listing;
            await using (var transaction = transactionScope.Begin())
            {
                listing = await bazaarItemDao.TryInsertOrUpdateAsync(new BazaarItemDto
                {
                    Amount = amount,
                    DateStart = clock.GetCurrentInstant(),
                    Duration = duration,
                    IsPackage = isPackage,
                    MedalUsed = hasMedal,
                    Price = price,
                    SellerId = characterId,
                    ItemInstanceId = item.Id
                });
                if ((listing == null) || !await RecordAsync(operationId, BazaarOperationType.Register,
                        listing.BazaarItemId, characterId, amount, price))
                {
                    throw new InvalidOperationException();
                }

                await transaction.CommitAsync();
            }

            bazaarRegistry.Hide(listing.BazaarItemId);
            bazaarRegistry.Register(listing.BazaarItemId, new BazaarLink
            {
                BazaarItem = listing,
                SellerName = characterName,
                ItemInstance = (ItemInstanceDto)item
            });
            return LanguageKey.OBJECT_IN_BAZAAR;
        }

        public async Task<bool> BuyBazaarAsync(Guid operationId, long id, short count, long price, long buyerId)
        {
            using var operationClaim = await OperationLock(operationId).AcquireAsync();
            using var claim = await ListingLock(id).AcquireAsync();
            var replay = await FindOperationAsync(operationId);
            if (replay != null)
            {
                return replay.Type == BazaarOperationType.Buy && replay.BazaarItemId == id
                    && replay.CharacterId == buyerId && replay.State != BazaarOperationState.Compensated;
            }

            var bzlink = bazaarRegistry.GetById(id);
            if (bzlink?.BazaarItem is not { } listing || bzlink.ItemInstance is not { } item
                || bazaarRegistry.IsHidden(id) || count <= 0 || listing.SellerId == buyerId
                || listing.Price != price || item.Amount < count
                || (listing.IsPackage && (count != listing.Amount || item.Amount != listing.Amount)))
            {
                return false;
            }

            if (!await CommitAsync(
                    () => SaveItemAmountAsync(item, (short)(item.Amount - count)),
                    () => RecordAsync(operationId, BazaarOperationType.Buy, id, buyerId, count, price)))
            {
                return false;
            }

            item.Amount -= count;
            return true;
        }

        public async Task<BazaarLink?> ModifyBazaarAsync(Guid operationId, long id, long sellerId, long price)
        {
            using var operationClaim = await OperationLock(operationId).AcquireAsync();
            using var claim = await ListingLock(id).AcquireAsync();
            var bzlink = bazaarRegistry.GetById(id);
            var replay = await FindOperationAsync(operationId);
            if (replay != null)
            {
                return replay.Type == BazaarOperationType.Modify && replay.BazaarItemId == id ? bzlink : null;
            }

            if (bzlink?.BazaarItem is not { } listing || bzlink.ItemInstance is not { } item
                || listing.SellerId != sellerId || price < 0 || bazaarRegistry.IsHidden(id)
                || listing.Amount != item.Amount || await PendingBuysAsync(id) > 0)
            {
                return null;
            }

            var updated = listing.Adapt<BazaarItemDto>();
            updated.Price = price;
            if (!await CommitAsync(
                    async () => await bazaarItemDao.TryInsertOrUpdateAsync(updated) != null,
                    () => RecordAsync(operationId, BazaarOperationType.Modify, id, sellerId, item.Amount, price,
                        BazaarOperationState.Settled)))
            {
                return null;
            }

            listing.Price = price;
            return bzlink;
        }

        public async Task<BazaarClaim?> ClaimBazaarAsync(Guid operationId, long id, long sellerId)
        {
            using var operationClaim = await OperationLock(operationId).AcquireAsync();
            using var claim = await ListingLock(id).AcquireAsync();
            var bzlink = bazaarRegistry.GetById(id);
            var replay = await FindOperationAsync(operationId);
            if (replay != null)
            {
                return replay.Type == BazaarOperationType.Claim && replay.BazaarItemId == id
                    && replay.CharacterId == sellerId && replay.State != BazaarOperationState.Compensated
                    && bzlink?.BazaarItem != null
                        ? ToClaim(bzlink, replay)
                        : null;
            }

            if (bzlink?.BazaarItem is not { } listing || bzlink.ItemInstance is not { } item
                || listing.SellerId != sellerId || bazaarRegistry.IsHidden(id))
            {
                return null;
            }

            var pending = await PendingBuysAsync(id);
            var sold = listing.Amount - item.Amount - pending;
            if ((item.Amount < 0) || (sold < 0) || ((sold == 0) && (item.Amount == 0)))
            {
                return null;
            }

            var gross = listing.Price * sold;
            var taxes = listing.MedalUsed ? 0 : (long)(gross * 0.10);
            var operation = NewOperation(operationId, BazaarOperationType.Claim, id, sellerId, item.Amount,
                listing.Price);
            operation.SoldAmount = (short)sold;
            operation.Gold = gross - taxes;
            operation.Taxes = taxes;
            if (!await CommitAsync(async () => await bazaarOperationDao.TryInsertOrUpdateAsync(operation) != null))
            {
                return null;
            }

            bazaarRegistry.Hide(id);
            return ToClaim(bzlink, operation);
        }

        public async Task<bool> SettleBazaarAsync(Guid operationId)
        {
            using var operationClaim = await OperationLock(operationId).AcquireAsync();
            var operation = await FindOperationAsync(operationId);
            if (operation == null)
            {
                return false;
            }

            using var claim = await ListingLock(operation.BazaarItemId).AcquireAsync();
            operation = await FindOperationAsync(operationId);
            if (operation?.State != BazaarOperationState.Pending)
            {
                return operation?.State == BazaarOperationState.Settled;
            }

            var bzlink = bazaarRegistry.GetById(operation.BazaarItemId);
            switch (operation.Type)
            {
                case BazaarOperationType.Claim when bzlink?.BazaarItem is { } listing && bzlink.ItemInstance is { } item:
                    var amount = (short)(listing.Amount - operation.SoldAmount - operation.Amount);
                    var left = (short)(item.Amount - operation.Amount);
                    if (amount <= 0)
                    {
                        if (!await CommitAsync(
                                async () => await bazaarItemDao.TryDeleteAsync(listing.BazaarItemId) != null,
                                async () => await itemInstanceDao.TryDeleteAsync(item.Id) != null,
                                () => SetStateAsync(operation, BazaarOperationState.Settled)))
                        {
                            throw new InvalidOperationException();
                        }

                        bazaarRegistry.Unregister(listing.BazaarItemId);
                    }
                    else
                    {
                        var updated = listing.Adapt<BazaarItemDto>();
                        updated.Amount = amount;
                        if (!await CommitAsync(
                                async () => await bazaarItemDao.TryInsertOrUpdateAsync(updated) != null,
                                () => SaveItemAmountAsync(item, left),
                                () => SetStateAsync(operation, BazaarOperationState.Settled)))
                        {
                            throw new InvalidOperationException();
                        }

                        listing.Amount = amount;
                        item.Amount = left;
                    }

                    bazaarRegistry.Show(operation.BazaarItemId);
                    return true;
                case BazaarOperationType.Claim:
                    return false;
                default:
                    if (!await CommitAsync(() => SetStateAsync(operation, BazaarOperationState.Settled)))
                    {
                        throw new InvalidOperationException();
                    }

                    if (operation.Type == BazaarOperationType.Register)
                    {
                        bazaarRegistry.Show(operation.BazaarItemId);
                    }

                    return true;
            }
        }

        public async Task<bool> CompensateBazaarAsync(Guid operationId)
        {
            using var operationClaim = await OperationLock(operationId).AcquireAsync();
            var operation = await FindOperationAsync(operationId);
            if (operation == null)
            {
                // The request this undoes has not arrived yet, or never will. Recording the id as
                // compensated makes a late arrival replay as refused instead of applying.
                return await CommitAsync(() => RecordAsync(operationId, BazaarOperationType.Buy, 0, 0, 0, 0,
                    BazaarOperationState.Compensated)) ? true : throw new InvalidOperationException();
            }

            using var claim = await ListingLock(operation.BazaarItemId).AcquireAsync();
            operation = await FindOperationAsync(operationId);
            if (operation?.State != BazaarOperationState.Pending)
            {
                return operation?.State == BazaarOperationState.Compensated;
            }

            var bzlink = bazaarRegistry.GetById(operation.BazaarItemId);
            switch (operation.Type)
            {
                case BazaarOperationType.Buy when bzlink?.ItemInstance is { } item:
                    var restored = (short)(item.Amount + operation.Amount);
                    if (!await CommitAsync(
                            () => SaveItemAmountAsync(item, restored),
                            () => SetStateAsync(operation, BazaarOperationState.Compensated)))
                    {
                        throw new InvalidOperationException();
                    }

                    item.Amount = restored;
                    return true;
                case BazaarOperationType.Register when bzlink?.BazaarItem is { } listing && bzlink.ItemInstance is { } item:
                    if (!await CommitAsync(
                            async () => await bazaarItemDao.TryDeleteAsync(listing.BazaarItemId) != null,
                            async () => await itemInstanceDao.TryDeleteAsync(item.Id) != null,
                            () => SetStateAsync(operation, BazaarOperationState.Compensated)))
                    {
                        throw new InvalidOperationException();
                    }

                    bazaarRegistry.Unregister(listing.BazaarItemId);
                    bazaarRegistry.Show(listing.BazaarItemId);
                    return true;
                case BazaarOperationType.Claim:
                    if (!await CommitAsync(() => SetStateAsync(operation, BazaarOperationState.Compensated)))
                    {
                        throw new InvalidOperationException();
                    }

                    bazaarRegistry.Show(operation.BazaarItemId);
                    return true;
                default:
                    return false;
            }
        }

        private static BazaarClaim ToClaim(BazaarLink bzlink, BazaarOperationDto operation)
        {
            ItemInstanceDto? returned = null;
            if ((operation.Amount > 0)
                && (bzlink.ItemInstance.Adapt(bzlink.ItemInstance.GetType(), bzlink.ItemInstance.GetType()) is ItemInstanceDto copy))
            {
                returned = copy;
                returned.Amount = operation.Amount;
            }

            return new BazaarClaim
            {
                Listing = bzlink.BazaarItem.Adapt<BazaarItemDto>(),
                Item = returned,
                SoldAmount = operation.SoldAmount,
                ReturnedAmount = operation.Amount,
                Gold = operation.Gold,
                Taxes = operation.Taxes
            };
        }

        private async Task<BazaarOperationDto?> FindOperationAsync(Guid operationId) =>
            await bazaarOperationDao.FirstOrDefaultAsync(s => s.BazaarOperationId == operationId);

        private async Task<int> PendingBuysAsync(long id) =>
            (bazaarOperationDao.Where(s => s.BazaarItemId == id && s.State == BazaarOperationState.Pending
                && s.Type == BazaarOperationType.Buy) ?? Enumerable.Empty<BazaarOperationDto>())
            .Sum(s => s.Amount);

        private BazaarOperationDto NewOperation(Guid operationId, BazaarOperationType type, long id, long characterId,
            short amount, long price, BazaarOperationState state = BazaarOperationState.Pending) => new()
        {
            BazaarOperationId = operationId,
            Type = type,
            State = state,
            BazaarItemId = id,
            CharacterId = characterId,
            Amount = amount,
            Price = price,
            CreatedAt = clock.GetCurrentInstant()
        };

        private async Task<bool> RecordAsync(Guid operationId, BazaarOperationType type, long id, long characterId,
            short amount, long price, BazaarOperationState state = BazaarOperationState.Pending) =>
            await bazaarOperationDao.TryInsertOrUpdateAsync(
                NewOperation(operationId, type, id, characterId, amount, price, state)) != null;

        private async Task<bool> SetStateAsync(BazaarOperationDto operation, BazaarOperationState state)
        {
            var updated = operation.Adapt<BazaarOperationDto>();
            updated.State = state;
            return await bazaarOperationDao.TryInsertOrUpdateAsync(updated) != null;
        }

        private async Task<bool> SaveItemAmountAsync(ItemInstanceDto item, short amount)
        {
            if (item.Adapt(item.GetType(), item.GetType()) is not IItemInstanceDto updated)
            {
                return false;
            }

            updated.Amount = amount;
            return await itemInstanceDao.TryInsertOrUpdateAsync(updated) != null;
        }

        // The registry is only touched after the commit, so memory never shows a change the
        // database rolled back. Settle and compensate throw on a failed commit instead of
        // refusing, so the channel keeps retrying them.
        private async Task<bool> CommitAsync(params Func<Task<bool>>[] steps)
        {
            await using var transaction = transactionScope.Begin();
            foreach (var step in steps)
            {
                if (!await step())
                {
                    return false;
                }
            }

            await transaction.CommitAsync();
            return true;
        }
    }
}

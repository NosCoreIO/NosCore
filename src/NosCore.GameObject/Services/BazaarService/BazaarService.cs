//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.More;
using NodaTime;
using NosCore.Core.Concurrency;
using System.Collections.Concurrent;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BazaarService
{
    public class BazaarService(IBazaarRegistry bazaarRegistry, IDao<BazaarItemDto, long> bazaarItemDao,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, IClock clock, List<ItemDto> items)
        : IBazaarService
    {
        // Channels reach these through BazaarHub, and its message handlers run concurrently, so
        // read-check-write on a listing is a lost update waiting to happen. One lock per listing
        // rather than one for the bazaar, so unrelated trades still run in parallel.
        private static readonly ConcurrentDictionary<long, AsyncLock> Claims = new();

        private static AsyncLock ClaimLock(long id) => Claims.GetOrAdd(id, _ => new AsyncLock());

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
                    : bazaarRegistry.GetAll();
            }

            if (typeFilter is { } type && type != BazaarListType.Default)
            {
                var links = bzlinks.Where(s => s.ItemInstance != null).ToList();
                var vnums = links.Select(s => s.ItemInstance!.ItemVNum)
                    .Concat(links.Select(s => (s.ItemInstance as BoxInstanceDto)?.HoldingVNum ?? 0))
                    .ToHashSet();
                var itemsByVNum = items.Where(s => vnums.Contains(s.VNum)).ToDictionary(s => s.VNum);
                var filter = new BazaarFilter(type, subTypeFilter ?? 0, levelFilter ?? 0, rareFilter ?? 0, upgradeFilter ?? 0);
                bzlinks = links.Where(s => itemsByVNum.TryGetValue(s.ItemInstance!.ItemVNum, out var item)
                    && filter.Matches(s.ItemInstance, item, itemsByVNum));
            }

            if (pageSize is { } size)
            {
                bzlinks = bzlinks.Skip((index ?? 0) * size).Take(size);
            }

            return bzlinks.ToList();
        }

        public async Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName, long? requestCharacterId = null)
        {
            using var claim = await ClaimLock(id).AcquireAsync();
            var bzlink = bazaarRegistry.GetById(id);
            if (bzlink == null)
            {
                throw new ArgumentException();
            }

            if ((bzlink.ItemInstance?.Amount - count < 0) || (count <= 0))
            {
                return false;
            }

            if (bzlink.ItemInstance?.Amount == count)
            {
                await bazaarItemDao.TryDeleteAsync(bzlink.BazaarItem!.BazaarItemId);
                bazaarRegistry.Unregister(bzlink.BazaarItem.BazaarItemId);
                await itemInstanceDao.TryDeleteAsync(bzlink.ItemInstance.Id);
            }
            else
            {
                var item = (IItemInstanceDto)bzlink.ItemInstance!;
                item.Amount -= count;
                await itemInstanceDao.TryInsertOrUpdateAsync(item);

                if (item.Amount <= 0)
                {
                    await bazaarItemDao.TryDeleteAsync(bzlink.BazaarItem!.BazaarItemId);
                    bazaarRegistry.Unregister(bzlink.BazaarItem.BazaarItemId);
                    await itemInstanceDao.TryDeleteAsync(item.Id);
                }
            }

            return true;
        }

        public async Task<LanguageKey> AddBazaarAsync(Guid itemInstanceId, long characterId, string? characterName, bool hasMedal, long price, bool isPackage, short duration, short amount)
        {
            var itemCount = bazaarRegistry.CountBySellerId(characterId);
            if (itemCount > 10 * (hasMedal ? 10 : 1) - 1)
            {
                return LanguageKey.LIMIT_EXCEEDED;
            }

            var item = await itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == itemInstanceId).ConfigureAwait(true);
            if ((item == null) || (item.Amount < amount) || (amount < 0) ||
                (price < 0))
            {
                throw new ArgumentException();
            }

            Guid itemId;
            if (item.Amount == amount)
            {
                itemId = item.Id;
            }
            else
            {
                itemId = item.Id;
                item.Amount -= amount;
                item = await itemInstanceDao.TryInsertOrUpdateAsync(item).ConfigureAwait(true);
                item!.Id = Guid.NewGuid();
            }

            item = await itemInstanceDao.TryInsertOrUpdateAsync(item).ConfigureAwait(true);

            var bazaarItem = new BazaarItemDto
            {
                Amount = amount,
                DateStart = clock.GetCurrentInstant(),
                Duration = duration,
                IsPackage = isPackage,
                MedalUsed = hasMedal,
                Price = price,
                SellerId = characterId,
                ItemInstanceId = item!.Id
            };
            bazaarItem = await bazaarItemDao.TryInsertOrUpdateAsync(bazaarItem).ConfigureAwait(true);
            bazaarRegistry.Register(bazaarItem.BazaarItemId,
                new BazaarLink
                {
                    BazaarItem = bazaarItem,
                    SellerName = characterName,
                    ItemInstance = (ItemInstanceDto)item
                });

            return LanguageKey.OBJECT_IN_BAZAAR;
        }

        public async Task<BazaarLink?> ModifyBazaarAsync(long id, Json.Patch.JsonPatch bzMod)
        {
            using var claim = await ClaimLock(id).AcquireAsync();
            var item = bazaarRegistry.GetById(id);
            if ((item?.BazaarItem == null) || (item.BazaarItem?.Amount != item.ItemInstance?.Amount))
            {
                return null;
            }

            var result = bzMod.Apply(JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(item, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))).RootElement.AsNode());
            item = JsonSerializer.Deserialize<BazaarLink>(result.Result, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
            var bz = item!.BazaarItem!;
            await bazaarItemDao.TryInsertOrUpdateAsync(bz).ConfigureAwait(true);
            bazaarRegistry.Update(item.BazaarItem!.BazaarItemId, item);
            return item;
        }
    }
}

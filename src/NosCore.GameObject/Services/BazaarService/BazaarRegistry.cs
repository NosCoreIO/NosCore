//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.WebApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.BazaarService
{
    public class BazaarRegistry : IBazaarRegistry
    {
        private readonly ConcurrentDictionary<long, BazaarLink> _bazaarItems;

        // Listings a pending registration or collection keeps away from buyers.
        private readonly ConcurrentDictionary<long, byte> _hidden = new();

        public BazaarRegistry(IDao<BazaarItemDto, long> bazaarItemDao,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, IDao<CharacterDto, long> characterDao,
            IDao<BazaarOperationDto, Guid> bazaarOperationDao)
        {
            foreach (var operation in bazaarOperationDao.Where(s => s.State == BazaarOperationState.Pending
                         && (s.Type == BazaarOperationType.Register || s.Type == BazaarOperationType.Claim))
                     ?? Enumerable.Empty<BazaarOperationDto>())
            {
                _hidden.TryAdd(operation.BazaarItemId, 0);
            }

            var billist = bazaarItemDao.LoadAll().ToList();
            var bzItemInstanceIds = billist.Select(o => o.ItemInstanceId).ToList();
            var bzCharacterIds = billist.Select(o => o.SellerId).ToList();
            var itemInstancelist = itemInstanceDao.Where(s => bzItemInstanceIds.Contains(s.Id))?.ToList() ?? new List<IItemInstanceDto?>();
            var characterList = characterDao.Where(s => bzCharacterIds.Contains(s.CharacterId))?.ToList() ?? new List<CharacterDto>();

            _bazaarItems = new ConcurrentDictionary<long, BazaarLink>(billist.ToDictionary(x => x.BazaarItemId,
                x => new BazaarLink
                {
                    ItemInstance = itemInstancelist.OfType<ItemInstanceDto>().First(s => s.Id == x.ItemInstanceId),
                    BazaarItem = x,
                    SellerName = characterList.First(s => s.CharacterId == x.SellerId).Name
                }));
        }

        public IEnumerable<BazaarLink> GetAll() => _bazaarItems.Values;

        public BazaarLink? GetById(long bazaarItemId) =>
            _bazaarItems.TryGetValue(bazaarItemId, out var link) ? link : null;

        public IEnumerable<BazaarLink> GetBySellerId(long sellerId) =>
            _bazaarItems.Values.Where(s => s.BazaarItem?.SellerId == sellerId);

        public void Register(long bazaarItemId, BazaarLink bazaarLink) =>
            _bazaarItems.TryAdd(bazaarItemId, bazaarLink);

        public bool Unregister(long bazaarItemId) =>
            _bazaarItems.TryRemove(bazaarItemId, out _);

        public void Update(long bazaarItemId, BazaarLink bazaarLink) =>
            _bazaarItems[bazaarItemId] = bazaarLink;

        public void Hide(long bazaarItemId) => _hidden.TryAdd(bazaarItemId, 0);

        public void Show(long bazaarItemId) => _hidden.TryRemove(bazaarItemId, out _);

        public bool IsHidden(long bazaarItemId) => _hidden.ContainsKey(bazaarItemId);

        public int CountBySellerId(long sellerId) =>
            _bazaarItems.Values.Count(o => o.BazaarItem?.SellerId == sellerId);
    }
}

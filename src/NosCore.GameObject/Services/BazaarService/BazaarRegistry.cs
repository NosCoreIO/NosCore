//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
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

        public BazaarRegistry(IDao<BazaarItemDto, long> bazaarItemDao,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, IDao<CharacterDto, long> characterDao)
        {
            var billist = bazaarItemDao.LoadAll().ToList();
            var bzItemInstanceIds = billist.Select(o => o.ItemInstanceId).ToList();
            var bzCharacterIds = billist.Select(o => o.SellerId).ToList();
            var itemInstancelist = itemInstanceDao.Where(s => bzItemInstanceIds.Contains(s!.Id))?.ToList() ?? new List<IItemInstanceDto?>();
            var characterList = characterDao.Where(s => bzCharacterIds.Contains(s.CharacterId))?.ToList() ?? new List<CharacterDto>();

            _bazaarItems = new ConcurrentDictionary<long, BazaarLink>(billist.ToDictionary(x => x.BazaarItemId,
                x => new BazaarLink
                {
                    ItemInstance = (ItemInstanceDto?)itemInstancelist.First(s => s!.Id == x.ItemInstanceId),
                    BazaarItem = x,
                    SellerName = characterList.First(s => s.CharacterId == x.SellerId).Name!
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

        public int CountBySellerId(long sellerId) =>
            _bazaarItems.Values.Count(o => o.BazaarItem?.SellerId == sellerId);
    }
}

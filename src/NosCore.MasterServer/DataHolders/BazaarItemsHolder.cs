using Mapster;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.WebApi;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.MasterServer.DataHolders
{
    public class BazaarItemsHolder
    {
        public ConcurrentDictionary<long, BazaarLink> BazaarItems { get; set; }

        public BazaarItemsHolder(IGenericDao<BazaarItemDto> bazaarItemDao, IGenericDao<IItemInstanceDto> itemInstanceDao, List<ItemDto> items, IGenericDao<CharacterDto> characterDao)
        {
            var billist = bazaarItemDao.LoadAll().ToList();
            var bzItemInstanceIds = billist.Select(o => o.ItemInstanceId).ToList();
            var bzCharacterIds = billist.Select(o => o.SellerId).ToList();
            var itemInstancelist = itemInstanceDao.Where(s => bzItemInstanceIds.Contains(s.Id)).ToList();
            var characterList = characterDao.Where(s => bzCharacterIds.Contains(s.CharacterId)).ToList();

            BazaarItems = new ConcurrentDictionary<long, BazaarLink>(billist.ToDictionary(x => x.BazaarItemId, x => new BazaarLink { ItemInstance = itemInstancelist.First(s => s.Id == x.ItemInstanceId).Adapt<ItemInstanceDto>(), BazaarItem = x, SellerName = characterList.First(s => s.CharacterId == x.SellerId).Name }));
        }
    }
}

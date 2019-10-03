using System;
using System.Collections.Generic;
using System.Net.Http;
using Mapster;
using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.HttpClients.WarehouseHttpClient
{
    public class WarehouseHttpClient : MasterServerHttpClient, IWarehouseHttpClient
    {
        private readonly IItemProvider _itemProvider;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;

        public WarehouseHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient, IItemProvider itemProvider,
            IGenericDao<IItemInstanceDto> itemInstanceDao)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/warehouse";
            RequireConnection = true;
            _itemProvider = itemProvider;
            _itemInstanceDao = itemInstanceDao;
        }

        public List<WarehouseItem> GetWarehouseItems(long characterId, WarehouseType warehouse)
        {
            var client = Connect();
            var response = client
                .GetAsync($"{ApiUrl}?id=null&characterId={characterId}&warehouseType={warehouse}")
                .Result;
            if (response.IsSuccessStatusCode)
            {
                var warehouseItems = new List<WarehouseItem>();
                var warehouselinks = JsonConvert.DeserializeObject<List<WarehouseLink>>(response.Content.ReadAsStringAsync().Result);
                foreach (var warehouselink in warehouselinks)
                {  
                    var warehouseItem = warehouselink.Warehouse.Adapt<WarehouseItem>();
                    var itemInstance = _itemInstanceDao.FirstOrDefault(s => s.Id == warehouselink.ItemInstance.Id);
                    warehouseItem.ItemInstance = _itemProvider.Convert(itemInstance);
                    warehouseItems.Add(warehouseItem);
                }
            }

            throw new ArgumentException();
        }

        public bool DepositItem(long characterCharacterId, WarehouseType warehouse, IItemInstance itemInstance, short slot)
        {
            throw new NotImplementedException();
        }

        public void DeleteWarehouseItem(long characterId, WarehouseType warehouse, short slot)
        {
            throw new NotImplementedException();
        }

        public List<WarehouseItem> MoveWarehouseItem(long characterId, WarehouseType warehouse, short slot, short destinationSlot)
        {
            throw new NotImplementedException();
        }
    }
}
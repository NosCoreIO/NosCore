using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ChickenAPI.Packets.Enumerations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.HttpClients.WarehouseHttpClient
{
    public class WarehouseHttpClient : MasterServerHttpClient, IWarehouseHttpClient
    {
        public WarehouseHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/warehouse";
            RequireConnection = true;
        }

        public List<WarehouseItem> GetWarehouseItems(long characterId, WarehouseType warehouse)
        {
            throw new NotImplementedException();
        }
    }
}
using System.Collections.Generic;
using NosCore.Data.Enumerations.Miniland;

namespace NosCore.GameObject.HttpClients.WarehouseHttpClient
{
    public interface IWarehouseHttpClient
    {
        List<WarehouseItem> GetWarehouseItems(long characterId, WarehouseType warehouse);
    }
}
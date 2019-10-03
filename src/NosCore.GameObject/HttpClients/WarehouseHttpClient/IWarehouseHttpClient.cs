using System.Collections.Generic;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.HttpClients.WarehouseHttpClient
{
    public interface IWarehouseHttpClient
    {
        List<WarehouseItem> GetWarehouseItems(long characterId, WarehouseType warehouse);

        bool DepositItem(long characterCharacterId, WarehouseType warehouse, IItemInstance itemInstance, short slot);

        void DeleteWarehouseItem(long characterId, WarehouseType warehouse, short slot);

        List<WarehouseItem> MoveWarehouseItem(long characterId, WarehouseType warehouse, short slot, short destinationSlot);

    }
}
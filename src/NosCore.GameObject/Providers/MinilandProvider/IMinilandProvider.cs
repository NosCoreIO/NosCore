using System;
using System.Collections.Generic;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.MapInstanceProvider;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public interface IMinilandProvider
    {
        Miniland GetMiniland(long character);
        void DeleteMiniland(long characterId);
        Miniland Initialize(Character character);
        List<Portal> GetMinilandPortals(long characterId);
        Miniland GetMinilandFromMapInstanceId(Guid mapInstanceId);
        void AddMinilandObject(MapDesignObject mapObject, long characterId, InventoryItemInstance minilandobject);
    }
}

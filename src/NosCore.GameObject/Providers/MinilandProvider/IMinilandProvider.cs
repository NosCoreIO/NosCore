using ChickenAPI.Packets.Enumerations;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.MapInstanceProvider;
using System;
using System.Collections.Generic;

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
        void SetState(long characterId, MinilandState state);
    }
}

using ChickenAPI.Packets.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Providers.MapInstanceProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public interface IMinilandProvider
    {
        Miniland GetMiniland(long character);
        void DeleteMiniland(long characterId);
        Miniland Initialize(Character character);
        List<Portal> GetMinilandPortals(long characterId);
        Miniland GetMinilandFromMapInstanceId(Guid mapInstanceId);
    }
}

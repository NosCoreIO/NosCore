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
        MinilandInfo GetMinilandInfo(long character);
        void DeleteMiniland(long characterId);
        MinilandInfo Initialize(long characterId, MinilandState state);
        List<Portal> GetMinilandPortals(long characterId);
        MinilandInfo GetMinilandInfoFromMapInstanceId(Guid mapInstanceId);
    }
}

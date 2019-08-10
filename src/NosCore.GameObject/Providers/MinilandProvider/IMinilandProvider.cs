using NosCore.GameObject.Providers.MapInstanceProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public interface IMinilandProvider
    {
        MapInstance GetMiniland(long character);
        void DeleteMiniland(long characterId);
        MapInstance Initialize(long characterId);
        List<Portal> GetMinilandPortals(long characterId);
    }
}

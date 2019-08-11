using ChickenAPI.Packets.Interfaces;
using NosCore.GameObject.Providers.MapInstanceProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class MapDesignObjectExtension
    {
        public static IPacket GenerateEffect(this MapDesignObject item) => item.GenerateEffect(0,  0,  0);
        public static IPacket GenerateEffect(this MapDesignObject item, short Effect, short MapX, short MapY)
        {
            var removed = item == null;
            //TODO add to chickenapi
            //return $"eff_g  {Effect} {MapX.ToString("00")}{MapY.ToString("00")} {MapX} {MapY} {(removed)}";
            return null;
        }
    }
}

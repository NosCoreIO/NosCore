using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.Miniland;
using NosCore.Data.Enumerations.Character;
using System;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public class MinilandInfo
    {
        public Guid MapInstanceId { get; set; }

        public MinilandState State { get; set; }

        public long Owner { get; set; }

        public string MinilandMessage { get; set; }

        public IPacket GenerateMlinfobr()
        {
            return new MlInfoBrPacket
            {
                //todo craft this packet
            };
        }

        public IPacket GenerateMlinfo()
        {
            return new MlinfoPacket
            {
                //todo craft this packet
            };
        }
    }
}

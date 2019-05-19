using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Relations;

namespace NosCore.Data.WebApi
{
    public class BlacklistRequest
    {
        public long CharacterId { get; set; }
        public BlInsPacket BlInsPacket { get; set; }
    }
}

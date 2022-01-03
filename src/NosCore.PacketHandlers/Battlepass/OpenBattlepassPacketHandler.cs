using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Battlepass
{
    public class OpenBattlepassPacketHandler : PacketHandler<BpOpenPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BpOpenPacket packet, ClientSession clientSession)
        {
            return Task.CompletedTask;
        }
    }
}
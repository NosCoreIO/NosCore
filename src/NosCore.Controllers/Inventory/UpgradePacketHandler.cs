using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Player;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Inventory
{
    public class UpgradePacketHandler : PacketHandler<UpgradePacket>, IWorldPacketHandler
    {
        public UpgradePacketHandler()
        {

        }

        public override void Execute(UpgradePacket packet, ClientSession clientSession)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets.Enumerations;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;

namespace NosCore.GameObject.Providers.UpgradeService
{
    public interface IUpgradeService
    {
        public void HandlePacket(UpgradePacketType type, ClientSession clientSession, InventoryItemInstance item1,
            InventoryItemInstance item2);
    }
}

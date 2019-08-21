//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using ChickenAPI.Packets.ClientPackets.Miniland;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Inventory
{
    public class UseobjPacketHandler : PacketHandler<UseObjPacket>, IWorldPacketHandler
    {
        public override void Execute(UseObjPacket useobjPacket, ClientSession clientSession)
        {
            //ClientSession client = ServerManager.Instance.Sessions.FirstOrDefault(s => s.Character?.Miniland == Session.Character.MapInstance);
            //IItemInstance minilandObjectItem = client?.Character.Inventory.LoadBySlotAndType<ItemInstance>(packet.Slot, InventoryType.Miniland);
            //if (minilandObjectItem != null)
            //{
            //    MapDesignObject minilandObject = client.Character.MapInstance.MapDesignObjects.FirstOrDefault(s => s.ItemInstanceId == minilandObjectItem.Id);
            //    if (minilandObject != null)
            //    {
            //        if (!minilandObjectItem.Item.IsWarehouse)
            //        {
            //            byte game = (byte)(minilandObject.ItemInstance.Item.EquipmentSlot == 0 ? 4 + minilandObject.ItemInstance.ItemVNum % 10 : (int)minilandObject.ItemInstance.Item.EquipmentSlot / 3);
            //            bool full = false;
            //            clientSession.SendPacket($"mlo_info {(client == Session ? 1 : 0)} {minilandObjectItem.ItemVNum} {packet.Slot} {Session.Character.MinilandPoint} {(minilandObjectItem.DurabilityPoint < 1000 ? 1 : 0)} {(full ? 1 : 0)} 0 {GetMinilandMaxPoint(game)[0]} {GetMinilandMaxPoint(game)[0] + 1} {GetMinilandMaxPoint(game)[1]} {GetMinilandMaxPoint(game)[1] + 1} {GetMinilandMaxPoint(game)[2]} {GetMinilandMaxPoint(game)[2] + 2} {GetMinilandMaxPoint(game)[3]} {GetMinilandMaxPoint(game)[3] + 1} {GetMinilandMaxPoint(game)[4]} {GetMinilandMaxPoint(game)[4] + 1} {GetMinilandMaxPoint(game)[5]}");
            //        }
            //        else
            //        {
            //            clientSession.SendPacket(new StashAllPacket());
            //        }
            //    }
            //}
        }
    }
}
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
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Miniland;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using System.Linq;

namespace NosCore.PacketHandlers.Inventory
{
    public class RmvobjPacketHandler : PacketHandler<RmvobjPacket>, IWorldPacketHandler
    {
        public override void Execute(RmvobjPacket rmvobjPacket, ClientSession clientSession)
        {
            var minilandobject = clientSession.Character.Inventory.LoadBySlotAndType(rmvobjPacket.Slot, NoscorePocketType.Miniland);
            if (minilandobject == null)
            {
                return;
            }
            if (clientSession.Character.MinilandState == MinilandState.Lock)
            {
                if (!clientSession.Character.MapInstance.MapDesignObjects.ContainsKey(minilandobject.Id))
                {
                    return;
                }

                var minilandObject = clientSession.Character.MapInstance.MapDesignObjects[minilandobject.Id];
                if (minilandobject.ItemInstance.Item.IsWarehouse)
                {
                    //todo warehouse
                    //clientSession.Character.WareHouseSize = 0;
                }
                clientSession.Character.MapInstance.MapDesignObjects.TryRemove(minilandobject.Id, out _);
                clientSession.SendPacket(minilandObject.GenerateEffect());
                clientSession.SendPacket(new MinilandPointPacket { MinilandPoint = minilandobject.ItemInstance.Item.MinilandObjectPoint, Unknown = 100 });
                clientSession.SendPacket(minilandObject.GenerateMapDesignObject(true));
            }
            else
            {
                clientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_NEED_LOCK, clientSession.Account.Language)
                });
            }
        }
    }
}
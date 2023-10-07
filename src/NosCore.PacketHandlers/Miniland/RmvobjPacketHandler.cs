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

using NosCore.Data.Enumerations;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland
{
    public class RmvobjPacketHandler(IMinilandService minilandProvider) : PacketHandler<RmvobjPacket>,
        IWorldPacketHandler
    {
        public override async Task ExecuteAsync(RmvobjPacket rmvobjPacket, ClientSession clientSession)
        {
            var minilandobject =
                clientSession.Character.InventoryService.LoadBySlotAndType(rmvobjPacket.Slot, NoscorePocketType.Miniland);
            if (minilandobject == null)
            {
                return;
            }

            if (minilandProvider.GetMiniland(clientSession.Character.CharacterId).State != MinilandState.Lock)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.RemoveOnlyLockMode
                }).ConfigureAwait(false);
                return;
            }

            if (!clientSession.Character.MapInstance.MapDesignObjects.ContainsKey(minilandobject.Id))
            {
                return;
            }

            var minilandObject = clientSession.Character.MapInstance.MapDesignObjects[minilandobject.Id];
            clientSession.Character.MapInstance.MapDesignObjects.TryRemove(minilandobject.Id, out _);
            await clientSession.SendPacketAsync(minilandObject.GenerateEffect(true)).ConfigureAwait(false);
            await clientSession.SendPacketAsync(new MinilandPointPacket
            { MinilandPoint = minilandobject.ItemInstance!.Item!.MinilandObjectPoint, Unknown = 100 }).ConfigureAwait(false);
            await clientSession.SendPacketAsync(minilandObject.GenerateMapDesignObject(true)).ConfigureAwait(false);
        }
    }
}
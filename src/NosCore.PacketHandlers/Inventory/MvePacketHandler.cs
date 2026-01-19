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
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;

namespace NosCore.PacketHandlers.Inventory
{
    public class MvePacketHandler : PacketHandler<MvePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MvePacket mvePacket, ClientSession clientSession)
        {
            var inv = clientSession.Character.InventoryService.MoveInPocket(mvePacket.Slot,
                (NoscorePocketType)mvePacket.InventoryType,
                (NoscorePocketType)mvePacket.DestinationInventoryType, mvePacket.DestinationSlot, false);
            await clientSession.SendPacketAsync(inv.GeneratePocketChange(mvePacket.DestinationInventoryType,
                mvePacket.DestinationSlot));
            await clientSession.SendPacketAsync(
                ((InventoryItemInstance?)null).GeneratePocketChange(mvePacket.InventoryType, mvePacket.Slot));
        }
    }
}
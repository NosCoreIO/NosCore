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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.WarehouseHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MinilandProvider;

namespace NosCore.PacketHandlers.Warehouse
{
    public class DepositPacketHandler : PacketHandler<DepositPacket>, IWorldPacketHandler
    {
        private readonly IWarehouseHttpClient _warehouseHttpClient;

        public DepositPacketHandler(IWarehouseHttpClient warehouseHttpClient)
        {
            _warehouseHttpClient = warehouseHttpClient;
        }

        public override Task Execute(DepositPacket depositPacket, ClientSession clientSession)
        {
            IItemInstance itemInstance = new ItemInstance();
            short slot = 0;
            var warehouseItems = _warehouseHttpClient.DepositItem(clientSession.Character!.CharacterId,
                WarehouseType.Warehouse, itemInstance, slot);
            return Task.CompletedTask;
        }
    }
}
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


using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class OpenShopEventHandler : INrunEventHandler
    {
        public bool Condition(NrunData item)
        {
            return (item.Packet.Runner == NrunRunnerType.OpenShop) && (item.Entity != null);
        }

        public Task ExecuteAsync(RequestData<NrunData> requestData)
        {
            return requestData.ClientSession.HandlePacketsAsync(new[]
            {
                new ShoppingPacket
                {
                    VisualType = requestData.Data.Packet.VisualType ?? 0,
                    VisualId = requestData.Data.Packet.VisualId ?? 0,
                    ShopType = requestData.Data.Packet.Type ?? 0,
                    Unknown = 0
                }
            });
        }
    }
}
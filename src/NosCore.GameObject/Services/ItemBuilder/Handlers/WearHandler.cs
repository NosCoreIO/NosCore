//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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


using NosCore.GameObject.Handling;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;
using System;

namespace NosCore.GameObject.Services.ItemBuilder.Handling
{
    public class WearHandler : IHandler<Item.Item, Tuple<IItemInstance, UseItemPacket>>
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public bool Condition(Item.Item item) => item.ItemType == ItemType.Weapon
        || item.ItemType == ItemType.Specialist
        || item.ItemType == ItemType.Jewelery
        || item.ItemType == ItemType.Armor
        || item.ItemType == ItemType.Fashion;

        public void Execute(RequestData<Tuple<IItemInstance, UseItemPacket>> requestData)
        {
            _logger.Debug($"wear item {requestData.Data.Item1.ItemVNum}");
        }
    }
}

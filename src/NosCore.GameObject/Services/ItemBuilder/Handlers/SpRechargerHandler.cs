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

using System;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Services.ItemBuilder.Handlers
{
    public class SpRechargerHandler : IHandler<Item.Item, Tuple<IItemInstance, UseItemPacket>>
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public bool Condition(Item.Item item) => item.ItemType == ItemType.Special && item.Effect == 150 || item.Effect == 151 || item.Effect == 152;

        public void Execute(RequestData<Tuple<IItemInstance, UseItemPacket>> requestData)
        {
            if (requestData.ClientSession.Character.SpAdditionPoint < 1000000)
            {
                var ItemInstance = requestData.Data.Item1;
                requestData.ClientSession.Character.Inventory.RemoveItemAmountFromInventory(1, ItemInstance.Id);
                requestData.ClientSession.SendPacket(ItemInstance.GeneratePocketChange(ItemInstance.Type, ItemInstance.Slot));
                var effvalue = ItemInstance.Item.EffectValue;
                if (ItemInstance.Item.Effect == 152)
                {
                    effvalue = ItemInstance.Item.EffectValue*10000;
                }
                requestData.ClientSession.Character.AddAdditionalSpPoints(effvalue);
            }
        }
    }
}
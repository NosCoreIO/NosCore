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
using NosCore.Configuration;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.ItemBuilder.Handlers
{
    public class SpRechargerHandler : IHandler<Item.Item, Tuple<IItemInstance, UseItemPacket>>
    {
        private readonly WorldConfiguration _worldConfiguration;
        public SpRechargerHandler(WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

        public bool Condition(Item.Item item) => item.ItemType == ItemType.Special && item.Effect >= ItemEffectType.DroppedSpRecharger && item.Effect <= ItemEffectType.CraftedSpRecharger;

        public void Execute(RequestData<Tuple<IItemInstance, UseItemPacket>> requestData)
        {
            if (requestData.ClientSession.Character.SpAdditionPoint < _worldConfiguration.MaxAdditionalSpPoints)
            {
                var itemInstance = requestData.Data.Item1;
                requestData.ClientSession.Character.Inventory.RemoveItemAmountFromInventory(1, itemInstance.Id);
                requestData.ClientSession.SendPacket(itemInstance.GeneratePocketChange(itemInstance.Type, itemInstance.Slot));
                requestData.ClientSession.Character.AddAdditionalSpPoints(itemInstance.Item.EffectValue);
            } else
            {
                requestData.ClientSession.Character.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.SP_ADDPOINTS_FULL, requestData.ClientSession.Character.Account.Language),
                    Type = MessageType.White
                });
            }
        }
    }
}
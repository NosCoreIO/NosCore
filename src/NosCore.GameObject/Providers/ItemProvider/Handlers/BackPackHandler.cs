﻿//  __  _  __    __   ___ __  ___ ___
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

using System;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using Serilog;

namespace NosCore.GameObject.Providers.ItemProvider.Handlers
{
    public class BackPackHandler : IEventHandler<Item.Item, Tuple<InventoryItemInstance, UseItemPacket>>
    {
        private readonly ILogger _logger;

        public BackPackHandler(ILogger logger)
        {
            _logger = logger;
        }

        public bool Condition(Item.Item item)
        {
            return (item.Effect == ItemEffectType.InventoryUpgrade);
        }

        public void Execute(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            if (requestData.ClientSession.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.BackPack))
            {
                var itemInstance = requestData.Data.Item1;

                requestData.ClientSession.Character.StaticBonusList.Add(new StaticBonusDto
                {
                    CharacterId = requestData.ClientSession.Character.CharacterId,
                    DateEnd = itemInstance.ItemInstance.Item.EffectValue == 0 ? (DateTime?)null : DateTime.Now.AddDays(itemInstance.ItemInstance.Item.EffectValue),
                    StaticBonusType = StaticBonusType.BackPack
                });

                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(string.Format(
                        Language.Instance.GetMessageFromKey(LanguageKey.EFFECT_ACTIVATED,
                            requestData.ClientSession.Account.Language),
                        itemInstance.ItemInstance.Item.Name[requestData.ClientSession.Account.Language]),
                    SayColorType.Green));
                requestData.ClientSession.SendPacket(
                    itemInstance.GeneratePocketChange((PocketType) itemInstance.Type, itemInstance.Slot));
                requestData.ClientSession.Character.Inventory.RemoveItemAmountFromInventory(1,
                    itemInstance.ItemInstanceId);

                requestData.ClientSession.Character.LoadExpensions();
            }
        }
    }
}
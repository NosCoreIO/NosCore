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

using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
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
        private readonly WorldConfiguration _conf;

        public BackPackHandler(WorldConfiguration conf)
        {
            _conf = conf;
        }

        public bool Condition(Item.Item item)
        {
            return (item.Effect == ItemEffectType.InventoryUpgrade || item.Effect == ItemEffectType.InventoryTicketUpgrade);
        }

        public Task Execute(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            var itemInstance = requestData.Data.Item1;

            if (itemInstance.ItemInstance!.Item!.Effect == ItemEffectType.InventoryUpgrade 
                && requestData.ClientSession.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BackPack))
            {
                return Task.CompletedTask;
            }

            if (itemInstance.ItemInstance.Item.Effect == ItemEffectType.InventoryTicketUpgrade
                && requestData.ClientSession.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.InventoryTicketUpgrade))
            {
                return Task.CompletedTask;
            }

            requestData.ClientSession.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = requestData.ClientSession.Character.CharacterId,
                DateEnd = itemInstance.ItemInstance.Item.EffectValue == 0 ? (DateTime?)null : SystemTime.Now().AddDays(itemInstance.ItemInstance.Item.EffectValue),
                StaticBonusType = itemInstance.ItemInstance.Item.Effect == ItemEffectType.InventoryTicketUpgrade ? StaticBonusType.InventoryTicketUpgrade : StaticBonusType.BackPack
            });

            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(string.Format(
                    Language.Instance.GetMessageFromKey(LanguageKey.EFFECT_ACTIVATED,
                        requestData.ClientSession.Account.Language),
                    itemInstance.ItemInstance.Item.Name[requestData.ClientSession.Account.Language]),
                SayColorType.Green));
            requestData.ClientSession.SendPacket(
                itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
            requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1,
                itemInstance.ItemInstanceId);

            requestData.ClientSession.Character.LoadExpensions();
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateExts(_conf));
            return Task.CompletedTask;
        }
    }
}
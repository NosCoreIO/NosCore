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

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ItemGenerationService.Handlers
{
    public class BazaarMedalsHandler(IClock clock) : IUseItemEventHandler
    {
        public bool Condition(Item.Item item)
        {
            return (item.Effect == ItemEffectType.SilverNosMerchantUpgrade)
                || (item.Effect == ItemEffectType.GoldNosMerchantUpgrade);
        }

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            if (requestData.ClientSession.Character.StaticBonusList.Any(s =>
                (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                (s.StaticBonusType == StaticBonusType.BazaarMedalSilver)))
            {
                return;
            }

            var itemInstance = requestData.Data.Item1;
            requestData.ClientSession.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = requestData.ClientSession.Character.CharacterId,
                DateEnd = clock.GetCurrentInstant().Plus(Duration.FromDays(itemInstance.ItemInstance.Item.EffectValue)),
                StaticBonusType = itemInstance.ItemInstance.Item.Effect == ItemEffectType.SilverNosMerchantUpgrade
                    ? StaticBonusType.BazaarMedalSilver : StaticBonusType.BazaarMedalGold
            });
            await requestData.ClientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = requestData.ClientSession.Character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.EffectActivated,
                ArgumentType = 2,
                Game18NArguments = { itemInstance.ItemInstance.Item.VNum.ToString() }
            });

            await requestData.ClientSession.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
            requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
        }
    }
}
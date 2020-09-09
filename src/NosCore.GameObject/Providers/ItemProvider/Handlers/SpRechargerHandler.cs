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
using System.Threading.Tasks;
using NosCore.Core.Configuration;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.GameObject.Providers.ItemProvider.Handlers
{
    public class SpRechargerEventHandler : IEventHandler<Item.Item, Tuple<InventoryItemInstance, UseItemPacket>>
    {
        private readonly WorldConfiguration _worldConfiguration;

        public SpRechargerEventHandler(WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

        public bool Condition(Item.Item item)
        {
            return (item.ItemType == ItemType.Special) &&
                (item.Effect >= ItemEffectType.DroppedSpRecharger) &&
                (item.Effect <= ItemEffectType.CraftedSpRecharger);
        }

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            if (requestData.ClientSession.Character.SpAdditionPoint < _worldConfiguration.MaxAdditionalSpPoints)
            {
                var itemInstance = requestData.Data.Item1;
                requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1,
                    itemInstance.ItemInstanceId);
                await requestData.ClientSession.SendPacketAsync(
                    itemInstance.GeneratePocketChange((PocketType) itemInstance.Type, itemInstance.Slot)).ConfigureAwait(false);
                await requestData.ClientSession.Character.AddAdditionalSpPointsAsync(itemInstance.ItemInstance!.Item!.EffectValue).ConfigureAwait(false);
            }
            else
            {
                await requestData.ClientSession.Character.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.SP_ADDPOINTS_FULL,
                        requestData.ClientSession.Account.Language),
                    Type = MessageType.White
                }).ConfigureAwait(false);
            }
        }
    }
}
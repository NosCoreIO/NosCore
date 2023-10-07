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

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Serilog;
using System;
using System.Threading.Tasks;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Networking;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.ItemGenerationService.Handlers
{
    public class VehicleEventHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ITransformationService transformationService)
        : IUseItemEventHandler
    {
        public bool Condition(Item.Item item)
        {
            return (item.ItemType == ItemType.Special) && (item.Effect == ItemEffectType.Vehicle);
        }

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            if (requestData.ClientSession.Character.InExchangeOrShop)
            {
                logger.Error(logLanguage[LogLanguageKey.CANT_USE_ITEM_IN_SHOP]);
                return;
            }

            if ((packet.Mode == 1) && !requestData.ClientSession.Character.IsVehicled)
            {
                await requestData.ClientSession.SendPacketAsync(new DelayPacket
                {
                    Type = DelayPacketType.Locomotion,
                    Delay = 3000,
                    Packet = requestData.ClientSession.Character.GenerateUseItem((PocketType)itemInstance.Type,
                        itemInstance.Slot,
                        2, 0)
                }).ConfigureAwait(false);
                return;
            }

            if ((packet.Mode == 2) && !requestData.ClientSession.Character.IsVehicled)
            {
                await transformationService.ChangeVehicleAsync(requestData.ClientSession.Character, itemInstance.ItemInstance.Item);
                return;
            }

            await transformationService.RemoveVehicleAsync(requestData.ClientSession.Character);
        }
    }
}
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
using NosCore.Networking;


namespace NosCore.GameObject.Services.ItemGenerationService.Handlers
{
    public class VehicleEventHandler : IUseItemEventHandler
    {
        private readonly ILogger _logger;

        public VehicleEventHandler(ILogger logger)
        {
            _logger = logger;
        }

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
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_USE_ITEM_IN_SHOP));
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
                requestData.ClientSession.Character.IsVehicled = true;
                requestData.ClientSession.Character.VehicleSpeed = itemInstance.ItemInstance!.Item!.Speed;
                requestData.ClientSession.Character.MorphUpgrade = 0;
                requestData.ClientSession.Character.MorphDesign = 0;
                requestData.ClientSession.Character.Morph =
                    itemInstance.ItemInstance.Item.SecondMorph == 0 ?
                        (short)((short)requestData.ClientSession.Character.Gender +
                            itemInstance.ItemInstance.Item.Morph) :
                        requestData.ClientSession.Character.Gender == GenderType.Male
                            ? itemInstance.ItemInstance.Item.Morph
                            : itemInstance.ItemInstance.Item.SecondMorph;

                await requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                    requestData.ClientSession.Character.GenerateEff(196)).ConfigureAwait(false);
                await requestData.ClientSession.Character.MapInstance.SendPacketAsync(requestData.ClientSession.Character
                    .GenerateCMode()).ConfigureAwait(false);
                await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateCond()).ConfigureAwait(false);
                return;
            }

            await requestData.ClientSession.Character.RemoveVehicleAsync().ConfigureAwait(false);
        }
    }
}
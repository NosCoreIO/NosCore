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
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Providers.ItemProvider.Handlers
{
    public class MinilandBellHandler : IEventHandler<Item.Item, Tuple<InventoryItemInstance, UseItemPacket>>
    {
        private readonly IMinilandProvider _minilandProvider;

        public MinilandBellHandler(IMinilandProvider minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public bool Condition(Item.Item item) => item.Effect == ItemEffectType.Teleport && item.EffectValue == 2;

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;

            if (requestData.ClientSession.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                await requestData.ClientSession.Character.SendPacketAsync(new SayPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_USE,
                        requestData.ClientSession.Account.Language),
                    Type = SayColorType.Yellow
                }).ConfigureAwait(false);
                return;
            }

            if (requestData.ClientSession.Character.IsVehicled)
            {
                await requestData.ClientSession.Character.SendPacketAsync(new SayPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_USE_IN_VEHICLE,
                        requestData.ClientSession.Account.Language),
                    Type = SayColorType.Yellow
                }).ConfigureAwait(false);
                return;
            }

            if (packet.Mode == 0)
            {
                await requestData.ClientSession.SendPacketAsync(new DelayPacket
                {
                    Type = 3,
                    Delay = 5000,
                    Packet = requestData.ClientSession.Character.GenerateUseItem((PocketType) itemInstance.Type,
                        itemInstance.Slot,
                        2, 0)
                }).ConfigureAwait(false);
                return;
            }

            requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await requestData.ClientSession.SendPacketAsync(
                itemInstance.GeneratePocketChange((PocketType) itemInstance.Type, itemInstance.Slot)).ConfigureAwait(false);
            var miniland = _minilandProvider.GetMiniland(requestData.ClientSession.Character.CharacterId);
            await requestData.ClientSession.ChangeMapInstanceAsync(miniland.MapInstanceId, 5, 8).ConfigureAwait(false);
        }
    }
}
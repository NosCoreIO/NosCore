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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Networking;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Services.GuriRunnerService.Handlers
{
    public class TitleGuriHandler(ICharacterPacketSystem characterPacketSystem, IInventoryPacketSystem inventoryPacketSystem)
        : IGuriEventHandler
    {
        public bool Condition(GuriPacket packet)
        {
            return (packet.Type == GuriPacketType.Title);
        }

        public async Task ExecuteAsync(RequestData<GuriPacket> requestData)
        {
            var player = requestData.ClientSession.Player;
            var inv = player.InventoryService.LoadBySlotAndType((short)(requestData.Data.VisualId ?? 0),
                NoscorePocketType.Main);
            if (inv?.ItemInstance?.Item?.ItemType != ItemType.Title ||
                player.Titles.Any(s => s.TitleType == inv.ItemInstance?.ItemVNum))
            {
                return;
            }

            player.Titles.Add(new TitleDto
            {
                Id = Guid.NewGuid(),
                TitleType = inv.ItemInstance.ItemVNum,
                Visible = false,
                Active = false,
                CharacterId = player.VisualId
            });
            await player.MapInstance.SendPacketAsync(characterPacketSystem.GenerateTitle(player)).ConfigureAwait(false);
            await requestData.ClientSession.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.TitleChangedOrHidden }).ConfigureAwait(false);
            player.InventoryService.RemoveItemAmountFromInventory(1, inv.ItemInstanceId);
            await requestData.ClientSession.SendPacketAsync(inventoryPacketSystem.GeneratePocketChange(inv, (PocketType)inv.Type, inv.Slot)).ConfigureAwait(false);
        }
    }
}
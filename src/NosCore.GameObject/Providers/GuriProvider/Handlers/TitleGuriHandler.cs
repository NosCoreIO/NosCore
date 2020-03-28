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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Providers.GuriProvider.Handlers
{
    public class TitleGuriHandler : IEventHandler<GuriPacket, GuriPacket>
    {
        public bool Condition(GuriPacket packet)
        {
            return (packet.Type == GuriPacketType.Title);
        }

        public async Task Execute(RequestData<GuriPacket> requestData)
        {
            var inv = requestData.ClientSession.Character.InventoryService.LoadBySlotAndType((short)requestData.Data.VisualId,
                NoscorePocketType.Main);
            if (inv?.ItemInstance.Item.ItemType != ItemType.Title ||
                requestData.ClientSession.Character.Titles.Any(s => s.TitleType == inv.ItemInstance.ItemVNum))
            {
                return;
            }

            requestData.ClientSession.Character.Titles.Add(new TitleDto
            {
                Id = Guid.NewGuid(),
                TitleType = inv.ItemInstance.ItemVNum,
                Visible = false,
                Active = false,
                CharacterId = requestData.ClientSession.Character.VisualId
            });
            await requestData.ClientSession.Character.MapInstance.SendPacket(requestData.ClientSession.Character.GenerateTitle());
            await requestData.ClientSession.SendPacket(new InfoPacket { Message = requestData.ClientSession.GetMessageFromKey(LanguageKey.WEAR_NEW_TITLE) });
            requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, inv.ItemInstanceId);
            await requestData.ClientSession.SendPacket(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot));
        }
    }
}
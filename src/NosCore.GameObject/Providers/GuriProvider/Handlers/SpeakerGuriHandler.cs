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
using ChickenAPI.Packets.ClientPackets.UI;
using ChickenAPI.Packets.Enumerations;
using Microsoft.IdentityModel.Logging;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;

namespace NosCore.GameObject.Providers.GuriProvider.Handlers
{
    public class SpeakerGuriHandler : IEventHandler<GuriPacket, GuriPacket>
    {
        public bool Condition(GuriPacket packet)
        {
            return packet.Type == GuriPacketType.TextInput && packet.Argument == 3;
        }

        private string CraftMessage(string message, string[] valuesplit)
        {
            message = valuesplit.Aggregate(message, (current, t) => current + t + " ");
            if (message.Length > 120)
            {
                message = message.Substring(0, 120);
            }

            return message.Trim();
        }

        public void Execute(RequestData<GuriPacket> requestData)
        {
            var inv = requestData.ClientSession.Character.InventoryService.LoadBySlotAndType((short)requestData.Data.VisualId,
                NoscorePocketType.Etc);
            if (inv?.ItemInstance.Item.Effect != ItemEffectType.Speaker)
            {
                return;
            }

            string data = requestData.Data.Value;
            string[] valuesplit = data.Split(' ');
            string message = $"<{Language.Instance.GetMessageFromKey(LanguageKey.SPEAKER, requestData.ClientSession.Account.Language)}> [{requestData.ClientSession.Character.Name}]:";
            if (requestData.Data.Data == 999 && short.TryParse(valuesplit[1], out var slot) && Enum.TryParse(typeof(NoscorePocketType), valuesplit[0], out var type))
            {
                var deeplink = requestData.ClientSession.Character.InventoryService.LoadBySlotAndType(slot, (NoscorePocketType)type);
                message = CraftMessage(message, valuesplit.Skip(2).ToArray()).Replace(' ', '|');
                Broadcaster.Instance.SendPacket(requestData.ClientSession.Character.GenerateSayItem(message, deeplink), new EveryoneBut(requestData.ClientSession.Channel.Id));
            }
            else
            {
                message = CraftMessage(message, valuesplit);
                Broadcaster.Instance.SendPacket(requestData.ClientSession.Character.GenerateSay(message, (SayColorType)13), new EveryoneBut(requestData.ClientSession.Channel.Id));
            }

            requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, inv.ItemInstanceId);
            requestData.ClientSession.Character.SendPacket(inv.GeneratePocketChange(PocketType.Etc, (short)requestData.Data.VisualId));
        }
    }
}
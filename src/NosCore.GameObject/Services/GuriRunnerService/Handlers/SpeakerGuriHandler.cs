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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Shared.I18N;
using NosCore.GameObject.Services.BroadcastService;

namespace NosCore.GameObject.Services.GuriRunnerService.Handlers
{
    public class SpeakerGuriHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry)
        : IGuriEventHandler
    {
        public bool Condition(GuriPacket packet)
        {
            return packet.Type == GuriPacketType.TextInput && packet.Argument == 3 && packet.VisualId != 0;
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

        public async Task ExecuteAsync(RequestData<GuriPacket> requestData)
        {
            var inv = requestData.ClientSession.Character.InventoryService.LoadBySlotAndType((short)(requestData.Data.VisualId ?? 0),
                NoscorePocketType.Etc);
            if (inv?.ItemInstance?.Item?.Effect != ItemEffectType.Speaker)
            {
                logger.Error(string.Format(logLanguage[LogLanguageKey.ITEM_NOT_FOUND], NoscorePocketType.Etc, (short)(requestData.Data.VisualId ?? 0)));
                return;
            }

            var data = requestData.Data.Value;
            string[] valuesplit = (data ?? string.Empty).Split(' ');
            string message = $"<{gameLanguageLocalizer[LanguageKey.SPEAKER, requestData.ClientSession.Account.Language]}> [{requestData.ClientSession.Character.Name}]:";
            if (requestData.Data.Data == 999)
            {
                InventoryItemInstance? deeplink = null;
                if (short.TryParse(valuesplit[1], out var slot) &
                    Enum.TryParse(typeof(NoscorePocketType), valuesplit[0], out var type))
                {
                    deeplink = requestData.ClientSession.Character.InventoryService.LoadBySlotAndType(slot, (NoscorePocketType)type!);
                }
                if (deeplink == null)
                {
                    logger.Error(string.Format(logLanguage[LogLanguageKey.ITEM_NOT_FOUND], type, slot));
                    return;
                }
                message = CraftMessage(message, valuesplit.Skip(2).ToArray()).Replace(' ', '|');
                await sessionRegistry.BroadcastPacketAsync(requestData.ClientSession.Character.GenerateSayItem(message, deeplink), requestData.ClientSession.Channel!.Id).ConfigureAwait(false);
            }
            else
            {
                message = CraftMessage(message, valuesplit);
                await sessionRegistry.BroadcastPacketAsync(requestData.ClientSession.Character.GenerateSay(message, (SayColorType)13), requestData.ClientSession.Channel!.Id).ConfigureAwait(false);
            }

            requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, inv.ItemInstanceId);
            await requestData.ClientSession.Character.SendPacketAsync(inv.GeneratePocketChange(PocketType.Etc, (short)(requestData.Data.VisualId ?? 0))).ConfigureAwait(false);
        }
    }
}
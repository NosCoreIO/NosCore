//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                await sessionRegistry.BroadcastPacketAsync(requestData.ClientSession.Character.GenerateSayItem(message, deeplink), requestData.ClientSession.Channel!.Id);
            }
            else
            {
                message = CraftMessage(message, valuesplit);
                await sessionRegistry.BroadcastPacketAsync(requestData.ClientSession.Character.GenerateSay(message, (SayColorType)13), requestData.ClientSession.Channel!.Id);
            }

            requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, inv.ItemInstanceId);
            await requestData.ClientSession.Character.SendPacketAsync(inv.GeneratePocketChange(PocketType.Etc, (short)(requestData.Data.VisualId ?? 0)));
        }
    }
}

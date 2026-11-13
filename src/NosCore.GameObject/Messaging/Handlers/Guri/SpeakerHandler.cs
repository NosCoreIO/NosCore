//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.PenaltyService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class SpeakerHandler(
        ILogger<SpeakerHandler> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IGameLanguageLocalizer gameLanguageLocalizer,
        ISessionRegistry sessionRegistry,
        IPenaltyService penaltyService)
    {
        private static string CraftMessage(string message, string[] valueSplit)
        {
            message = valueSplit.Aggregate(message, (current, t) => current + t + " ");
            if (message.Length > 120)
            {
                message = message.Substring(0, 120);
            }
            return message.Trim();
        }

        private Task BroadcastToListenersAsync(IPacket packet, string senderChannelId) =>
            Task.WhenAll(sessionRegistry.GetCharacters(c => !c.HeroChatBlocked && c.Channel?.Id != senderChannelId)
                .Select(async c =>
                {
                    try
                    {
                        await c.SendPacketAsync(packet);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Speaker message to {CharacterId} failed", c.CharacterId);
                    }
                }));

        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.TextInput || packet.Argument != 3 || packet.VisualId == 0)
            {
                return;
            }

            var session = evt.ClientSession;
            var inv = session.Character.InventoryService.LoadBySlotAndType(
                (short)(packet.VisualId ?? 0), NoscorePocketType.Etc);
            if (inv?.ItemInstance?.Item?.Effect != ItemEffectType.Speaker)
            {
                logger.LogError(string.Format(logLanguage[LogLanguageKey.ITEM_NOT_FOUND],
                    NoscorePocketType.Etc, (short)(packet.VisualId ?? 0)));
                return;
            }

            if (await penaltyService.GetMuteLeftAsync(session.Account.AccountId) is { } muteLeft)
            {
                await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Red,
                    Game18NConstString.RemainingPunishment, 4, (long)Math.Ceiling(muteLeft.TotalMinutes)));
                return;
            }

            var data = packet.Value;
            var valueSplit = (data ?? string.Empty).Split(' ');
            var message = $"<{gameLanguageLocalizer[LanguageKey.SPEAKER, session.Account.Language]}> [{session.Character.Name}]:";
            if (packet.Data == 999)
            {
                InventoryItemInstance? deeplink = null;
                if (short.TryParse(valueSplit[1], out var slot) &
                    Enum.TryParse<NoscorePocketType>(valueSplit[0], out var type))
                {
                    deeplink = session.Character.InventoryService.LoadBySlotAndType(slot, type);
                }
                if (deeplink == null)
                {
                    logger.LogError(string.Format(logLanguage[LogLanguageKey.ITEM_NOT_FOUND], type, slot));
                    return;
                }
                message = CraftMessage(message, valueSplit.Skip(2).ToArray()).Replace(' ', '|');
                await BroadcastToListenersAsync(session.Character.GenerateSayItem(message, deeplink), session.ConnectedChannel.Id);
            }
            else
            {
                message = CraftMessage(message, valueSplit);
                await BroadcastToListenersAsync(session.Character.GenerateSay(message, (SayColorType)13), session.ConnectedChannel.Id);
            }

            session.Character.InventoryService.RemoveItemAmountFromInventory(1, inv.ItemInstanceId);
            await session.SendPacketAsync(inv.GeneratePocketChange(PocketType.Etc, (short)(packet.VisualId ?? 0)));
        }
    }
}

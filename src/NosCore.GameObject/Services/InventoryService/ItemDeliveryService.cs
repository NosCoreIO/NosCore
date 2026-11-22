//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MailService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.InventoryService
{
    public class ItemDeliveryService(IMailHub mailHub, IClock clock, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
        IDao<EquipmentOptionDto, Guid> equipmentOptionDao, ILogger<ItemDeliveryService> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IItemDeliveryService
    {
        public async Task<bool> DeliverAsync(PlayerComponentBundle player, IItemInstance item)
        {
            var amount = item.Amount;
            var slots = player.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, player.CharacterId));
            if (slots is { Count: > 0 })
            {
                await player.SendPacketsAsync(slots.Select(s => s.GeneratePocketChange(s.Type.ToPocketType(), s.Slot)));
                await player.SendPacketAsync(player.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, item.ItemVNum.ToString(), amount));
                return true;
            }

            await SendToParcelBoxAsync(player, item, amount);
            return false;
        }

        public async Task ReturnConsumedAsync(PlayerComponentBundle player, InventoryItemInstance source, short amount)
        {
            var (slot, pocket) = (source.Slot, source.Type);
            var (restored, left) = player.InventoryService.RestoreConsumed(source, amount);
            if (left > 0)
            {
                await SendToParcelBoxAsync(player, source.ItemInstance, left);
            }

            await player.SendPacketAsync(player.InventoryService.LoadBySlotAndType(slot, pocket)
                .GeneratePocketChange(pocket.ToPocketType(), slot));
            await player.SendPacketsAsync(restored.Where(s => s.Slot != slot || s.Type != pocket)
                .Select(s => s.GeneratePocketChange(s.Type.ToPocketType(), s.Slot)));
        }

        // The parcel carries a stored copy of the item itself, so its options and what it holds come back with it.
        private async Task SendToParcelBoxAsync(PlayerComponentBundle player, IItemInstance item, short amount)
        {
            var parcel = (IItemInstance)item.Clone();
            parcel.Id = Guid.NewGuid();
            parcel.Amount = amount;
            List<EquipmentOptionDto> options = [];
            if (parcel is WearableInstance wearable)
            {
                options = ((item as WearableInstance)?.EquipmentOptions
                        ?? equipmentOptionDao.Where(o => o.WearableInstanceId == item.Id)?.ToList() ?? [])
                    .Select(o => o.CopyFor(parcel.Id)).ToList();
                wearable.EquipmentOptions = options;
            }

            Exception? failure = null;
            try
            {
                if (await StoreAsync(parcel, options) && await mailHub.SendMailAsync(GiftHelper.GenerateMailRequest(clock, player,
                        player.CharacterId, parcel, null, null, null, null, false, null, null)))
                {
                    return;
                }
            }
            catch (Exception e)
            {
                failure = e;
            }

            // A refused mail removes its attachment, so the copy is written again for whoever recovers it.
            var kept = false;
            try
            {
                kept = await StoreAsync(parcel, options);
            }
            catch (Exception e)
            {
                failure ??= e;
            }

            if (kept)
            {
                logger.LogError(failure, logLanguage[LogLanguageKey.PARCEL_RETURN_FAILED], parcel.ItemVNum, amount, parcel.Rare,
                    parcel.Upgrade, player.CharacterId, parcel.Id);
                return;
            }

            logger.LogError(failure, logLanguage[LogLanguageKey.PARCEL_NOT_STORED], parcel.ItemVNum, amount,
                player.CharacterId, JsonSerializer.Serialize(parcel, parcel.GetType().BaseType ?? typeof(object), DetailJson),
                JsonSerializer.Serialize(options, DetailJson));
        }

        private static readonly JsonSerializerOptions DetailJson =
            new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        private async Task<bool> StoreAsync(IItemInstance parcel, List<EquipmentOptionDto> options) =>
            await itemInstanceDao.TryInsertOrUpdateAsync(parcel) != null
            && (options.Count == 0 || await equipmentOptionDao.TryInsertOrUpdateAsync(options));
    }
}

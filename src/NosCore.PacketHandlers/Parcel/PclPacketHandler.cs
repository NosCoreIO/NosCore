//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Parcel
{
    public class PclPacketHandler(IMailHub mailHttpClient, IItemGenerationService itemProvider,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, IDao<EquipmentOptionDto, Guid> equipmentOptionDao)
        : PacketHandler<PclPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PclPacket getGiftPacket, ClientSession clientSession)
        {
            if (getGiftPacket.GiftId < 0)
            {
                return;
            }

            var isCopy = getGiftPacket.Type == 2;
            var characterId = clientSession.Character.VisualId;
            var mails = await mailHttpClient.GetMails(getGiftPacket.GiftId, characterId, isCopy);
            var mail = mails.FirstOrDefault(s => s.MailId == getGiftPacket.GiftId);
            if (mail == null)
            {
                return;
            }

            if ((getGiftPacket.Type == 4) && (mail.ItemInstance != null))
            {
                var itemInstance = await itemInstanceDao.FirstOrDefaultAsync(s => s.Id == mail.ItemInstance.Id);
                if (itemInstance == null)
                {
                    return;
                }

                var item = itemProvider.Convert(itemInstance);
                // Removing the mail removes its attachment and the options stored against it.
                var options = item is WearableInstance
                    ? equipmentOptionDao.Where(o => o.WearableInstanceId == item.Id)?.ToList() ?? []
                    : [];
                item.Id = Guid.NewGuid();
                var inventory = clientSession.Character.InventoryService;
                var amount = item.Amount;
                // The parcel is only removed once the item is in the bag with its options stored, so a bag
                // that fills up meanwhile or a failed write leaves the parcel where it was.
                var slots = inventory.AddItemToPocket(InventoryItemInstance.Create(item, clientSession.Character.CharacterId));
                if (slots is not { Count: > 0 })
                {
                    await SendNotEnoughSpaceAsync(clientSession);
                    return;
                }

                if (!await StoreOptionsAsync(item, options)
                    || !await mailHttpClient.DeleteMailAsync(getGiftPacket.GiftId, characterId, isCopy))
                {
                    await TakeBackAsync(inventory, slots, amount, item, options.Count > 0);
                    return;
                }

                await clientSession.SendPacketsAsync(slots.Select(s => s.GeneratePocketChange(s.Type.ToPocketType(), s.Slot)));
                await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Red, Game18NConstString.ParcelReceived,
                    2, item.ItemVNum.ToString(), amount));

                await clientSession.SendPacketAsync(
                    new ParcelPacket { Type = ParcelPacketType.Received, Box = MailBoxType.Received, Id = (short)getGiftPacket.GiftId });
            }
            else if (getGiftPacket.Type == 5)
            {
                if (await mailHttpClient.DeleteMailAsync(getGiftPacket.GiftId, characterId, isCopy))
                {
                    await clientSession.SendPacketAsync(new ParcelPacket { Type = ParcelPacketType.Deleted, Box = MailBoxType.Received, Id = (short)getGiftPacket.GiftId });
                }
            }
        }

        private async Task<bool> StoreOptionsAsync(IItemInstance item, List<EquipmentOptionDto> options)
        {
            if (options.Count == 0 || item is not WearableInstance wearable)
            {
                return true;
            }

            wearable.EquipmentOptions = options.Select(o => o.CopyFor(item.Id)).ToList();
            return await itemInstanceDao.TryInsertOrUpdateAsync(item) != null
                && await equipmentOptionDao.TryInsertOrUpdateAsync(wearable.EquipmentOptions);
        }

        // The parcel is still there, so what was placed for it comes back out of the bag.
        private async Task TakeBackAsync(IInventoryService inventory, List<InventoryItemInstance> slots, short amount,
            IItemInstance item, bool storedOptions)
        {
            foreach (var stack in Enumerable.Reverse(slots))
            {
                var taken = (short)Math.Min(amount, stack.ItemInstance.Amount);
                if (taken > 0 && inventory.TryConsume(stack, taken))
                {
                    amount -= taken;
                }
            }

            if (storedOptions)
            {
                _ = await itemInstanceDao.TryDeleteAsync(item.Id);
            }
        }

        private static async Task SendNotEnoughSpaceAsync(ClientSession clientSession)
        {
            await clientSession.SendPacketAsync(new ParcelPacket { Type = ParcelPacketType.NotEnoughSpace, Box = MailBoxType.Received, Id = 0 });
            await clientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.NotEnoughSpace
            });
        }
    }
}

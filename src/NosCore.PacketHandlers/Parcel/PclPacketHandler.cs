//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
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
            IDao<IItemInstanceDto?, Guid> itemInstanceDao)
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
                var itemInstance = await itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == mail.ItemInstance.Id);
                if (itemInstance == null)
                {
                    return;
                }

                var item = itemProvider.Convert(itemInstance);
                item.Id = Guid.NewGuid();
                var inventory = clientSession.Character.InventoryService;
                if (!inventory.CanAddItem(item.ItemVNum)
                    && !inventory.EnoughPlace(new List<IItemInstance> { item }, item.Item.Type))
                {
                    await SendNotEnoughSpaceAsync(clientSession);
                    return;
                }

                if (!await mailHttpClient.DeleteMailAsync(getGiftPacket.GiftId, characterId, isCopy))
                {
                    return;
                }

                var newInv = inventory
                    .AddItemToPocket(InventoryItemInstance.Create(item, clientSession.Character.CharacterId))?
                    .FirstOrDefault();
                if (newInv == null)
                {
                    await SendNotEnoughSpaceAsync(clientSession);
                    return;
                }

                await clientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = clientSession.Character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.ParcelReceived,
                    ArgumentType = 2,
                    Game18NArguments = { newInv.ItemInstance.Item.VNum.ToString(), newInv.ItemInstance.Amount }
                });

                await clientSession.SendPacketAsync(
                    new ParcelPacket { Type = 2, Unknown = 1, Id = (short)getGiftPacket.GiftId });
            }
            else if (getGiftPacket.Type == 5)
            {
                if (await mailHttpClient.DeleteMailAsync(getGiftPacket.GiftId, characterId, isCopy))
                {
                    await clientSession.SendPacketAsync(new ParcelPacket { Type = 7, Unknown = 1, Id = (short)getGiftPacket.GiftId });
                }
            }
        }

        private static async Task SendNotEnoughSpaceAsync(ClientSession clientSession)
        {
            await clientSession.SendPacketAsync(new ParcelPacket { Type = 5, Unknown = 1, Id = 0 });
            await clientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.NotEnoughSpace
            });
        }
    }
}

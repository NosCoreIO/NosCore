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
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
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
            var isCopy = getGiftPacket.Type == 2;
            var mails = await mailHttpClient.GetMails(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
            var mail = mails.FirstOrDefault();
            if (mail == null)
            {
                return;
            }

            if ((getGiftPacket.Type == 4) && (mail.ItemInstance != null))
            {
                var itemInstance = await itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == mail.ItemInstance.Id);
                var item = itemProvider.Convert(itemInstance!);
                item.Id = Guid.NewGuid();
                var newInv = clientSession.Character.InventoryService
                    .AddItemToPocket(InventoryItemInstance.Create(item, clientSession.Character.CharacterId))?
                    .FirstOrDefault();
                if (newInv != null)
                {
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
                    await mailHttpClient.DeleteMailAsync(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
                }
                else
                {
                    await clientSession.SendPacketAsync(new ParcelPacket { Type = 5, Unknown = 1, Id = 0 });
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.NotEnoughSpace
                    });
                }
            }
            else if (getGiftPacket.Type == 5)
            {
                await clientSession.SendPacketAsync(new ParcelPacket { Type = 7, Unknown = 1, Id = (short)getGiftPacket.GiftId });
                await mailHttpClient.DeleteMailAsync(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
            }
        }
    }
}

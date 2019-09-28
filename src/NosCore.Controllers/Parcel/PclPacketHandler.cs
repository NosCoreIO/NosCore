using System;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Parcel;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Parcel;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;

namespace NosCore.PacketHandlers.Parcel
{
    public class PclPacketHandler : PacketHandler<PclPacket>, IWorldPacketHandler
    {
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IItemProvider _itemProvider;
        private readonly IMailHttpClient _mailHttpClient;

        public PclPacketHandler(IMailHttpClient mailHttpClient, IItemProvider itemProvider,
            IGenericDao<IItemInstanceDto> itemInstanceDao)
        {
            _mailHttpClient = mailHttpClient;
            _itemProvider = itemProvider;
            _itemInstanceDao = itemInstanceDao;
        }

        public override void Execute(PclPacket getGiftPacket, ClientSession clientSession)
        {
            var isCopy = getGiftPacket.Type == 2;
            var mail = _mailHttpClient.GetGift(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
            if (mail == null)
            {
                return;
            }

            if ((getGiftPacket.Type == 4) && (mail.ItemInstance != null))
            {
                var itemInstance = _itemInstanceDao.FirstOrDefault(s => s.Id == mail.ItemInstance.Id);
                var item = _itemProvider.Convert(itemInstance);
                item.Id = Guid.NewGuid();
                var newInv = clientSession.Character.Inventory
                    .AddItemToPocket(InventoryItemInstance.Create(item, clientSession.Character.CharacterId))
                    .FirstOrDefault();
                if (newInv != null)
                {
                    clientSession.SendPacket(clientSession.Character.GenerateSay(
                        string.Format(
                            Language.Instance.GetMessageFromKey(LanguageKey.ITEM_RECEIVED,
                                clientSession.Account.Language),
                            newInv.ItemInstance.Item.Name, newInv.ItemInstance.Amount), SayColorType.Green));
                    clientSession.SendPacket(
                        new ParcelPacket {Type = 2, Unknown = 1, Id = (short) getGiftPacket.GiftId});
                    _mailHttpClient.DeleteGift(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
                }
                else
                {
                    clientSession.SendPacket(new ParcelPacket {Type = 5, Unknown = 1, Id = 0});
                    clientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                            clientSession.Account.Language),
                        Type = 0
                    });
                }
            }
            else if (getGiftPacket.Type == 5)
            {
                clientSession.SendPacket(new ParcelPacket {Type = 7, Unknown = 1, Id = (short) getGiftPacket.GiftId});
                _mailHttpClient.DeleteGift(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
            }
        }
    }
}
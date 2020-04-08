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
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.MailHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;

namespace NosCore.PacketHandlers.Parcel
{
    public class PclPacketHandler : PacketHandler<PclPacket>, IWorldPacketHandler
    {
        private readonly IDao<IItemInstanceDto, Guid> _itemInstanceDao;
        private readonly IItemProvider _itemProvider;
        private readonly IMailHttpClient _mailHttpClient;

        public PclPacketHandler(IMailHttpClient mailHttpClient, IItemProvider itemProvider,
            IDao<IItemInstanceDto, Guid> itemInstanceDao)
        {
            _mailHttpClient = mailHttpClient;
            _itemProvider = itemProvider;
            _itemInstanceDao = itemInstanceDao;
        }

        public override async Task ExecuteAsync(PclPacket getGiftPacket, ClientSession clientSession)
        {
            var isCopy = getGiftPacket.Type == 2;
            var mail = await _mailHttpClient.GetGiftAsync(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
            if (mail == null)
            {
                return;
            }

            if ((getGiftPacket.Type == 4) && (mail.ItemInstance != null))
            {
                var itemInstance = await _itemInstanceDao.FirstOrDefaultAsync(s => s.Id == mail.ItemInstance.Id).ConfigureAwait(false);
                var item = _itemProvider.Convert(itemInstance!);
                item.Id = Guid.NewGuid();
                var newInv = clientSession.Character.InventoryService
                    .AddItemToPocket(InventoryItemInstance.Create(item, clientSession.Character.CharacterId))
                    .FirstOrDefault();
                if (newInv != null)
                {
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateSay(
                        string.Format(
                            GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_RECEIVED,
                                clientSession.Account.Language),
                            newInv.ItemInstance!.Item!.Name, newInv.ItemInstance.Amount), SayColorType.Green)).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(
                        new ParcelPacket {Type = 2, Unknown = 1, Id = (short) getGiftPacket.GiftId}).ConfigureAwait(false);
                    await _mailHttpClient.DeleteGiftAsync(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
                }
                else
                {
                    await clientSession.SendPacketAsync(new ParcelPacket {Type = 5, Unknown = 1, Id = 0}).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new MsgPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                            clientSession.Account.Language),
                        Type = 0
                    }).ConfigureAwait(false);
                }
            }
            else if (getGiftPacket.Type == 5)
            {
                await clientSession.SendPacketAsync(new ParcelPacket {Type = 7, Unknown = 1, Id = (short) getGiftPacket.GiftId}).ConfigureAwait(false);
                await _mailHttpClient.DeleteGiftAsync(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
            }
        }
    }
}
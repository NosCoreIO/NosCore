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
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.MailHttpClient;
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
    public class PclPacketHandler : PacketHandler<PclPacket>, IWorldPacketHandler
    {
        private readonly IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private readonly IItemGenerationService _itemProvider;
        private readonly IMailHttpClient _mailHttpClient;

        public PclPacketHandler(IMailHttpClient mailHttpClient, IItemGenerationService itemProvider,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao)
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
                var itemInstance = await _itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == mail.ItemInstance.Id).ConfigureAwait(false);
                var item = _itemProvider.Convert(itemInstance!);
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
                        Game18NArguments = new object[] { newInv.ItemInstance.Item.VNum, newInv.ItemInstance.Amount }
                    }).ConfigureAwait(false);
                    
                    await clientSession.SendPacketAsync(
                        new ParcelPacket { Type = 2, Unknown = 1, Id = (short)getGiftPacket.GiftId }).ConfigureAwait(false);
                    await _mailHttpClient.DeleteGiftAsync(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
                }
                else
                {
                    await clientSession.SendPacketAsync(new ParcelPacket { Type = 5, Unknown = 1, Id = 0 }).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.NotEnoughSpace
                    }).ConfigureAwait(false);
                }
            }
            else if (getGiftPacket.Type == 5)
            {
                await clientSession.SendPacketAsync(new ParcelPacket { Type = 7, Unknown = 1, Id = (short)getGiftPacket.GiftId }).ConfigureAwait(false);
                await _mailHttpClient.DeleteGiftAsync(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy).ConfigureAwait(false);
            }
        }
    }
}
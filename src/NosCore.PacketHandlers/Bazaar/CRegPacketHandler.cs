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

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CRegPacketHandler(IOptions<WorldConfiguration> configuration, IBazaarHub bazaarHttpClient,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao)
        : PacketHandler<CRegPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CRegPacket cRegPacket, ClientSession clientSession)
        {
            var medal = clientSession.Character.StaticBonusList.FirstOrDefault(s =>
                (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));

            var price = cRegPacket.Price * cRegPacket.Amount;
            var taxmax = price > 100000 ? price / 200 : 500;
            var taxmin = price >= 4000 ? 60 + (price - 4000) / 2000 * 30 > 10000 ? 10000
                : 60 + (price - 4000) / 2000 * 30 : 50;
            var tax = medal == null ? taxmax : taxmin;
            var maxGold = configuration.Value.MaxGoldAmount;
            if (clientSession.Character.Gold < tax)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughGold
                }).ConfigureAwait(false);
                return;
            }

            var it = clientSession.Character.InventoryService!.LoadBySlotAndType(cRegPacket.Slot,
                cRegPacket.Inventory == 4 ? 0 : (NoscorePocketType)cRegPacket.Inventory);
            if ((it?.ItemInstance == null) || !it.ItemInstance.Item!.IsSoldable || (it.ItemInstance.BoundCharacterId != null) ||
                (cRegPacket.Amount > it.ItemInstance.Amount))
            {
                return;
            }

            var maxPrice = medal == null ? 1000000 : maxGold;

            if (price > maxPrice)
            {
                await clientSession.SendPacketAsync(new ModaliPacket
                {
                    Type = 1,
                    Message = Game18NConstString.NotExceedMaxPrice,
                    ArgumentType = 4,
                    Game18NArguments = { maxPrice }
                }).ConfigureAwait(false);
                return;
            }

            if ((medal == null) && (cRegPacket.Durability > 1))
            {
                return;
            }

            short duration;
            switch (cRegPacket.Durability)
            {
                case 1:
                    duration = 24;
                    break;

                case 2:
                    duration = 168;
                    break;

                case 3:
                    duration = 360;
                    break;

                case 4:
                    duration = 720;
                    break;

                default:
                    return;
            }

            var bazar = clientSession.Character.InventoryService.LoadBySlotAndType(cRegPacket.Slot,
                cRegPacket.Inventory == 4 ? NoscorePocketType.Equipment : (NoscorePocketType)cRegPacket.Inventory);
            if (bazar?.ItemInstance == null)
            {
                return;
            }
            IItemInstanceDto bazaaritem = bazar.ItemInstance;
            bazaaritem = (await itemInstanceDao.TryInsertOrUpdateAsync(bazaaritem).ConfigureAwait(false))!;

            var result = await bazaarHttpClient.AddBazaarAsync(new BazaarRequest
            {
                ItemInstanceId = bazaaritem.Id,
                CharacterId = clientSession.Character.CharacterId,
                CharacterName = clientSession.Character.Name,
                HasMedal = medal != null,
                Price = cRegPacket.Price,
                IsPackage = cRegPacket.IsPackage,
                Duration = duration,
                Amount = cRegPacket.Amount
            }).ConfigureAwait(false);

            switch (result)
            {
                case LanguageKey.LIMIT_EXCEEDED:
                    await clientSession.SendPacketAsync(new ModaliPacket
                    {
                        Type = 1,
                        Message = Game18NConstString.ListedMaxItemsNumber
                    }).ConfigureAwait(false);
                    break;

                case LanguageKey.OBJECT_IN_BAZAAR:
                    if (bazar.ItemInstance.Amount == cRegPacket.Amount)
                    {
                        await inventoryItemInstanceDao.TryDeleteAsync(bazar.Id).ConfigureAwait(false);
                        clientSession.Character.InventoryService.DeleteById(bazar.ItemInstanceId);
                    }
                    else
                    {
                        clientSession.Character.InventoryService.RemoveItemAmountFromInventory(cRegPacket.Amount,
                            bazar.ItemInstanceId);
                    }

                    await clientSession.SendPacketAsync(((InventoryItemInstance?)null).GeneratePocketChange(
                        cRegPacket.Inventory == 4 ? PocketType.Equipment : (PocketType)cRegPacket.Inventory,
                        cRegPacket.Slot)).ConfigureAwait(false);
                    clientSession.Character.Gold -= tax;
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateGold()).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.ItemAddedToBazar
                    }).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.ItemAddedToBazar
                    }).ConfigureAwait(false);

                    await clientSession.SendPacketAsync(new RCRegPacket { Type = VisualType.Player }).ConfigureAwait(false);
                    break;
            }
        }
    }
}
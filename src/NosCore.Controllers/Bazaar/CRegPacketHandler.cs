﻿//  __  _  __    __   ___ __  ___ ___  
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
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Bazaar;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Bazaar;
using ChickenAPI.Packets.ServerPackets.UI;
using Mapster;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CRegPacketHandler : PacketHandler<CRegPacket>, IWorldPacketHandler
    {
        private readonly WorldConfiguration _configuration;
        private readonly IBazaarHttpClient _bazaarHttpClient;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IGenericDao<InventoryItemInstanceDto> _inventoryItemInstanceDao;

        public CRegPacketHandler(WorldConfiguration configuration, IBazaarHttpClient bazaarHttpClient, IGenericDao<IItemInstanceDto> itemInstanceDao, IGenericDao<InventoryItemInstanceDto> inventoryItemInstanceDao)
        {
            _configuration = configuration;
            _bazaarHttpClient = bazaarHttpClient;
            _itemInstanceDao = itemInstanceDao;
            _inventoryItemInstanceDao = inventoryItemInstanceDao;
        }

        public override void Execute(CRegPacket cRegPacket, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                return;
            }

            var medal = clientSession.Character.StaticBonusList.FirstOrDefault(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

            long price = cRegPacket.Price * cRegPacket.Amount;
            long taxmax = price > 100000 ? price / 200 : 500;
            long taxmin = price >= 4000 ? (60 + (price - 4000) / 2000 * 30 > 10000 ? 10000 : 60 + (price - 4000) / 2000 * 30) : 50;
            long tax = medal == null ? taxmax : taxmin;
            long maxGold = _configuration.MaxGoldAmount;
            if (clientSession.Character.Gold < tax || cRegPacket.Amount <= 0 || clientSession.Character.InExchangeOrShop)
            {
                return;
            }
            var it = clientSession.Character.Inventory.LoadBySlotAndType(cRegPacket.Slot, cRegPacket.Inventory == 4 ? 0 : (NoscorePocketType)cRegPacket.Inventory);
            if (it == null || !it.ItemInstance.Item.IsSoldable || it.ItemInstance.BoundCharacterId != null)
            {
                return;
            }

            if (price > (medal == null ? 100000000 : maxGold))
            {
                clientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.PRICE_EXCEEDED,
                        clientSession.Account.Language)
                });
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
            var bazar = clientSession.Character.Inventory.LoadBySlotAndType(cRegPacket.Slot, cRegPacket.Inventory == 4 ? NoscorePocketType.Equipment : (NoscorePocketType)cRegPacket.Inventory);
            IItemInstanceDto bazaaritem = bazar.ItemInstance;
            _itemInstanceDao.InsertOrUpdate(ref bazaaritem);

            var result = _bazaarHttpClient.AddBazaar(new BazaarRequest
            {
                ItemInstanceId = bazar.ItemInstance.Id,
                CharacterId = clientSession.Character.CharacterId,
                HasMedal = medal != null,
                Price = cRegPacket.Price,
                IsPackage = cRegPacket.IsPackage != 0,
                Duration = duration,
                Amount = cRegPacket.Amount
            });

            switch (result)
            {
                case LanguageKey.LIMIT_EXCEEDED:
                    clientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.LIMIT_EXCEEDED, clientSession.Account.Language)
                    });
                    break;

                case LanguageKey.OBJECT_IN_BAZAAR:
                    if (bazar.ItemInstance.Amount == cRegPacket.Amount)
                    {
                        _inventoryItemInstanceDao.Delete(bazar.Id);
                        clientSession.Character.Inventory.DeleteById(bazar.ItemInstanceId);
                    }
                    else
                    {
                        clientSession.Character.Inventory.RemoveItemAmountFromInventory(cRegPacket.Amount,
                            bazar.ItemInstanceId);
                    }

                    clientSession.SendPacket(bazar.GeneratePocketChange(cRegPacket.Inventory == 4 ? PocketType.Equipment : (PocketType)cRegPacket.Inventory, cRegPacket.Slot));
                    clientSession.Character.Gold -= tax;
                    clientSession.SendPacket(clientSession.Character.GenerateGold());

                    clientSession.SendPacket(clientSession.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.OBJECT_IN_BAZAAR,
                        clientSession.Account.Language), SayColorType.Yellow));
                    clientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.OBJECT_IN_BAZAAR,
                            clientSession.Account.Language)
                    });

                    clientSession.SendPacket(new RCRegPacket { Type = VisualType.Player });
                    break;
            }
        }
    }
}
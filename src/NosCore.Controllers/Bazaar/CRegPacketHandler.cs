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

using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CRegPacketHandler : PacketHandler<CRegPacket>, IWorldPacketHandler
    {
        private readonly IBazaarHttpClient _bazaarHttpClient;
        private readonly WorldConfiguration _configuration;
        private readonly IGenericDao<InventoryItemInstanceDto> _inventoryItemInstanceDao;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;

        public CRegPacketHandler(WorldConfiguration configuration, IBazaarHttpClient bazaarHttpClient,
            IGenericDao<IItemInstanceDto> itemInstanceDao,
            IGenericDao<InventoryItemInstanceDto> inventoryItemInstanceDao)
        {
            _configuration = configuration;
            _bazaarHttpClient = bazaarHttpClient;
            _itemInstanceDao = itemInstanceDao;
            _inventoryItemInstanceDao = inventoryItemInstanceDao;
        }

        public override async Task Execute(CRegPacket cRegPacket, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                return;
            }

            var medal = clientSession.Character.StaticBonusList.FirstOrDefault(s =>
                (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));

            var price = cRegPacket.Price * cRegPacket.Amount;
            var taxmax = price > 100000 ? price / 200 : 500;
            var taxmin = price >= 4000 ? 60 + (price - 4000) / 2000 * 30 > 10000 ? 10000
                : 60 + (price - 4000) / 2000 * 30 : 50;
            var tax = medal == null ? taxmax : taxmin;
            var maxGold = _configuration.MaxGoldAmount;
            if (clientSession.Character.Gold < tax)
            {
                await clientSession.SendPacket(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY,
                        clientSession.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            if ((cRegPacket.Amount <= 0) || clientSession.Character.InExchangeOrShop || cRegPacket.Inventory > (byte)PocketType.Etc)
            {
                return;
            }

            var it = clientSession.Character.InventoryService!.LoadBySlotAndType(cRegPacket.Slot,
                cRegPacket.Inventory == 4 ? 0 : (NoscorePocketType) cRegPacket.Inventory);
            if ((it?.ItemInstance == null) || !it.ItemInstance.Item!.IsSoldable || (it.ItemInstance.BoundCharacterId != null) ||
                (cRegPacket.Amount > it.ItemInstance.Amount))
            {
                return;
            }

            if (price > (medal == null ? 100000000 : maxGold))
            {
                await clientSession.SendPacket(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.PRICE_EXCEEDED,
                        clientSession.Account.Language)
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
                cRegPacket.Inventory == 4 ? NoscorePocketType.Equipment : (NoscorePocketType) cRegPacket.Inventory);
            if (bazar?.ItemInstance == null)
            {
                return;
            }
            IItemInstanceDto bazaaritem = bazar.ItemInstance;
            _itemInstanceDao.InsertOrUpdate(ref bazaaritem);

            var result = await _bazaarHttpClient.AddBazaar(new BazaarRequest
            {
                ItemInstanceId = bazar.ItemInstance.Id,
                CharacterId = clientSession.Character.CharacterId,
                CharacterName = clientSession.Character.Name,
                HasMedal = medal != null,
                Price = cRegPacket.Price,
                IsPackage = cRegPacket.IsPackage != 0,
                Duration = duration,
                Amount = cRegPacket.Amount
            }).ConfigureAwait(false);

            switch (result)
            {
                case LanguageKey.LIMIT_EXCEEDED:
                    await clientSession.SendPacket(new MsgPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.LIMIT_EXCEEDED,
                            clientSession.Account.Language)
                    }).ConfigureAwait(false);
                    break;

                case LanguageKey.OBJECT_IN_BAZAAR:
                    if (bazar.ItemInstance.Amount == cRegPacket.Amount)
                    {
                        _inventoryItemInstanceDao.Delete(bazar.Id);
                        clientSession.Character.InventoryService.DeleteById(bazar.ItemInstanceId);
                    }
                    else
                    {
                        clientSession.Character.InventoryService.RemoveItemAmountFromInventory(cRegPacket.Amount,
                            bazar.ItemInstanceId);
                    }

                    await clientSession.SendPacket(((InventoryItemInstance?)null).GeneratePocketChange(
                        cRegPacket.Inventory == 4 ? PocketType.Equipment : (PocketType) cRegPacket.Inventory,
                        cRegPacket.Slot)).ConfigureAwait(false);
                    clientSession.Character.Gold -= tax;
                    await clientSession.SendPacket(clientSession.Character.GenerateGold()).ConfigureAwait(false);

                    await clientSession.SendPacket(clientSession.Character.GenerateSay(GameLanguage.Instance.GetMessageFromKey(
                        LanguageKey.OBJECT_IN_BAZAAR,
                        clientSession.Account.Language), SayColorType.Yellow)).ConfigureAwait(false);
                    await clientSession.SendPacket(new MsgPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.OBJECT_IN_BAZAAR,
                            clientSession.Account.Language)
                    }).ConfigureAwait(false);

                    await clientSession.SendPacket(new RCRegPacket {Type = VisualType.Player}).ConfigureAwait(false);
                    break;
            }
        }
    }
}
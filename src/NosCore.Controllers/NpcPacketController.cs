//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Database.Entities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.NRunAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;
using MapNpc = NosCore.GameObject.MapNpc;

namespace NosCore.Controllers
{
    public class NpcPacketController : PacketController
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly WorldConfiguration _worldConfiguration;
        private readonly NrunAccessService _nRunAccessService;

        [UsedImplicitly]
        public NpcPacketController()
        {
        }

        public NpcPacketController(WorldConfiguration worldConfiguration, NrunAccessService nRunAccessService)
        {
            _worldConfiguration = worldConfiguration;
            _nRunAccessService = nRunAccessService;
        }

        /// <summary>
        /// npc_req packet
        /// </summary>
        /// <param name="requestNpcPacket"></param>
        public void ShowShop(RequestNpcPacket requestNpcPacket)
        {
            IRequestableEntity requestableEntity;
            switch (requestNpcPacket.Type)
            {
                case VisualType.Player:
                    requestableEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == requestNpcPacket.TargetId);
                    break;
                case VisualType.Npc:
                    requestableEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == requestNpcPacket.TargetId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALTYPE_UNKNOWN), requestNpcPacket.Type);
                    return;
            }
            if (requestableEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            requestableEntity.Requests.OnNext(new RequestData(Session));
        }

        /// <summary>
        /// nRunPacket packet
        /// </summary>
        /// <param name="nRunPacket"></param>
        public void NRun(NrunPacket nRunPacket)
        {
            MapNpc requestableEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == nRunPacket.NpcId);

            if (requestableEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }
            _nRunAccessService.NRunLaunch(Session, new Tuple<MapNpc, NrunPacket>(requestableEntity, nRunPacket));
        }

        /// <summary>
        /// shopping packet
        /// </summary>
        /// <param name="shoppingPacket"></param>
        public void Shopping(ShoppingPacket shoppingPacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                //TODO log
                return;
            }

            IAliveEntity aliveEntity;
            switch (shoppingPacket.Type)
            {
                case VisualType.Player:
                    aliveEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == shoppingPacket.TargetId);
                    break;
                case VisualType.Npc:
                    aliveEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == shoppingPacket.TargetId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALTYPE_UNKNOWN), shoppingPacket.Type);
                    return;
            }
            if (aliveEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            var shopRate = Session.Character.GenerateShopRates();
            Session.SendPacket(aliveEntity.GenerateNInv(shopRate.Item1, shoppingPacket.ShopType, shopRate.Item2));
        }

        /// <summary>
        /// sell packet
        /// </summary>
        /// <param name="sellPacket"></param>
        public void SellShop(SellPacket sellPacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                //TODO log
                return;
            }

            if (sellPacket.Amount.HasValue && sellPacket.Slot.HasValue)
            {
                PocketType type = (PocketType)sellPacket.Data;

                var inv = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(sellPacket.Slot.Value, type);
                if (inv == null || sellPacket.Amount.Value > inv.Amount)
                { 
                    //TODO log
                    return;
                }

                if (!inv.Item.IsSoldable)
                {
                    Session.SendPacket(new SMemoPacket { Type = 2, Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_SOLDABLE, Session.Account.Language) });
                    return;
                }
                long price = inv.Item.ItemType == ItemType.Sell ? inv.Item.Price : inv.Item.Price / 20;

                if (Session.Character.Gold + price * sellPacket.Amount.Value > _worldConfiguration.MaxGoldAmount)
                {
                    Session.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
                            Session.Account.Language),
                        Type = 0
                    });
                    return;
                }
                Session.Character.Gold += price * sellPacket.Amount.Value;
                Session.SendPacket(new SMemoPacket { Type = 1, Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SELL_ITEM_VALIDE, Session.Account.Language), inv.Item.Name, sellPacket.Amount.Value) });
                
                Session.Character.Inventory.RemoveItemAmountFromInventory(sellPacket.Amount.Value, inv.Id);
                Session.SendPacket(Session.Character.GenerateGold());
            }
            else
            {
               //TODO sell skill
            }
        }


        /// <summary>
        /// buy packet
        /// </summary>
        /// <param name="buyPacket"></param>
        public void BuyShop(BuyPacket buyPacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                //TODO log
                return;
            }

            if (buyPacket.Amount > _worldConfiguration.MaxItemAmount)
            {
                //TODO log
                return;
            }

            IAliveEntity aliveEntity;
            switch (buyPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == buyPacket.VisualId);
                    break;
                case VisualType.Npc:
                    aliveEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == buyPacket.VisualId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALTYPE_UNKNOWN), buyPacket.VisualType);
                    return;
            }
            if (aliveEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            Session.Character.Buy(aliveEntity.Shop, buyPacket.Slot, buyPacket.Amount);
        }
    }
}
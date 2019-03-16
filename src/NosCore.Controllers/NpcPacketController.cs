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
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.NRunProvider;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using Serilog;
using Shop = NosCore.GameObject.Shop;
using ShopItem = NosCore.GameObject.ShopItem;

namespace NosCore.Controllers
{
    public class NpcPacketController : PacketController
    {
        private readonly ILogger _logger;
        private readonly INrunProvider _nRunProvider;
        private readonly WorldConfiguration _worldConfiguration;

        [UsedImplicitly]
        public NpcPacketController()
        {
        }

        public NpcPacketController(WorldConfiguration worldConfiguration, INrunProvider nRunProvider, ILogger logger)
        {
            _worldConfiguration = worldConfiguration;
            _nRunProvider = nRunProvider;
            _logger = logger;
        }

        /// <summary>
        ///     npc_req packet
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
                    requestableEntity =
                        Session.Character.MapInstance.Npcs.Find(s => s.VisualId == requestNpcPacket.TargetId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        requestNpcPacket.Type);
                    return;
            }

            if (requestableEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            requestableEntity.Requests.OnNext(new RequestData(Session));
        }

        /// <summary>
        ///     nRunPacket packet
        /// </summary>
        /// <param name="nRunPacket"></param>
        public void NRun(NrunPacket nRunPacket)
        {
            IAliveEntity aliveEntity;
            switch (nRunPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == nRunPacket.VisualId);
                    break;
                case VisualType.Npc:
                    aliveEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == nRunPacket.VisualId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        nRunPacket.Type);
                    return;
            }

            if (aliveEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            _nRunProvider.NRunLaunch(Session, new Tuple<IAliveEntity, NrunPacket>(aliveEntity, nRunPacket));
        }

        /// <summary>
        ///     shopping packet
        /// </summary>
        /// <param name="shoppingPacket"></param>
        public void Shopping(ShoppingPacket shoppingPacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                //TODO log
                return;
            }

            var shopRate = new Tuple<double, byte>(0, 0);
            IAliveEntity aliveEntity;
            switch (shoppingPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == shoppingPacket.VisualId);
                    break;
                case VisualType.Npc:
                    shopRate = Session.Character.GenerateShopRates();
                    aliveEntity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == shoppingPacket.VisualId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        shoppingPacket.VisualType);
                    return;
            }

            if (aliveEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }


            Session.SendPacket(aliveEntity.GenerateNInv(shopRate.Item1, shoppingPacket.ShopType, shopRate.Item2));
        }

        public void CreateShop(MShopPacket mShopPacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                //todo log
                return;
            }

            var portal = Session.Character.MapInstance.Portals.Find(port =>
                Heuristic.Octile(Math.Abs(Session.Character.PositionX - port.SourceX),
                    Math.Abs(Session.Character.PositionY - port.SourceY)) <= 6);
            if (portal != null)
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NEAR_PORTAL,
                        Session.Account.Language),
                    Type = 0
                });
                return;
            }

            if (Session.Character.Group != null && Session.Character.Group?.Type != GroupType.Group)
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED_IN_RAID,
                        Session.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            if (!Session.Character.MapInstance.ShopAllowed)
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED,
                        Session.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            switch (mShopPacket.Type)
            {
                case CreateShopPacketType.Open:
                    Session.Character.Shop = new Shop();
                    sbyte shopSlot = -1;
                    foreach (var item in mShopPacket.ItemList)
                    {
                        shopSlot++;
                        if (item.Amount == 0)
                        {
                            continue;
                        }

                        var inv = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(item.Slot, item.Type);
                        if (inv == null)
                        {
                            //log
                            continue;
                        }

                        if (inv.Amount < item.Amount)
                        {
                            //todo log
                            return;
                        }

                        if (!inv.Item.IsTradable || inv.BoundCharacterId != null)
                        {
                            Session.SendPacket(new ShopEndPacket {Type = ShopEndType.Closed});
                            Session.SendPacket(Session.Character.GenerateSay(
                                Language.Instance.GetMessageFromKey(LanguageKey.SHOP_ONLY_TRADABLE_ITEMS,
                                    Session.Account.Language),
                                SayColorType.Yellow));
                            Session.Character.Shop = null;
                            return;
                        }

                        Session.Character.Shop.ShopItems.TryAdd(shopSlot,
                            new ShopItem
                            {
                                Amount = item.Amount,
                                Price = item.Price,
                                Slot = (byte)shopSlot,
                                Type = 0,
                                ItemInstance = inv
                            });
                    }

                    if (Session.Character.Shop.ShopItems.Count == 0)
                    {
                        Session.SendPacket(new ShopEndPacket {Type = ShopEndType.Closed});
                        Session.SendPacket(Session.Character.GenerateSay(
                            Language.Instance.GetMessageFromKey(LanguageKey.SHOP_EMPTY, Session.Account.Language),
                            SayColorType.Yellow));
                        Session.Character.Shop = null;
                        return;
                    }

                    Session.Character.Shop.Session = Session;
                    Session.Character.Shop.MenuType = 3;
                    Session.Character.Shop.ShopId = 501;
                    Session.Character.Shop.Size = 60;
                    Session.Character.Shop.Name = string.IsNullOrWhiteSpace(mShopPacket.Name) ?
                        Language.Instance.GetMessageFromKey(LanguageKey.SHOP_PRIVATE_SHOP, Session.Account.Language) :
                        mShopPacket.Name.Substring(0, Math.Min(mShopPacket.Name.Length, 20));

                    Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GenerateShop());
                    Session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.SHOP_OPEN,
                            Session.Account.Language)
                    });

                    Session.Character.Requests.Subscribe(data =>
                        data.ClientSession.SendPacket(Session.Character.GenerateNpcReq(Session.Character.Shop.ShopId)));
                    Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GeneratePFlag(),
                        new EveryoneBut(Session.Channel.Id));
                    Session.Character.IsSitting = true;
                    Session.Character.LoadSpeed();
                    Session.SendPacket(Session.Character.GenerateCond());
                    Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GenerateRest());
                    break;
                case CreateShopPacketType.Close:
                    Session.Character.CloseShop();
                    break;
                case CreateShopPacketType.Create:
                    Session.SendPacket(new IshopPacket());
                    break;
                default:
                    //todo log
                    return;
            }
        }

        /// <summary>
        ///     sell packet
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
                PocketType type = (PocketType) sellPacket.Data;

                var inv = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(sellPacket.Slot.Value, type);
                if (inv == null || sellPacket.Amount.Value > inv.Amount)
                {
                    //TODO log
                    return;
                }

                if (!inv.Item.IsSoldable)
                {
                    Session.SendPacket(new SMemoPacket
                    {
                        Type = SMemoType.Error,
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_SOLDABLE,
                            Session.Account.Language)
                    });
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
                Session.SendPacket(new SMemoPacket
                {
                    Type = SMemoType.Success,
                    Message = string.Format(
                        Language.Instance.GetMessageFromKey(LanguageKey.SELL_ITEM_VALIDE, Session.Account.Language),
                        inv.Item.Name,
                        sellPacket.Amount.Value
                    )
                });

                Session.Character.Inventory.RemoveItemAmountFromInventory(sellPacket.Amount.Value, inv.Id);
                Session.SendPacket(Session.Character.GenerateGold());
            }
            else
            {
                //TODO sell skill
            }
        }


        /// <summary>
        ///     buy packet
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
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        buyPacket.VisualType);
                    return;
            }

            if (aliveEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            Session.Character.Buy(aliveEntity.Shop, buyPacket.Slot, buyPacket.Amount);
        }
    }
}
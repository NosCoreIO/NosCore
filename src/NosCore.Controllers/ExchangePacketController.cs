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
using System.Text;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Buff;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Controllers
{
    public class ExchangePacketController : PacketController
    {
        private readonly WorldConfiguration _worldConfiguration;
        private readonly ExchangeService _exchangeService;
        private readonly IItemBuilderService _itemBuilderService;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        
        public ExchangePacketController(WorldConfiguration worldConfiguration, ExchangeService exchangeService, IItemBuilderService itemBuilderService)
        {
            _worldConfiguration = worldConfiguration;
            _exchangeService = exchangeService;
            _itemBuilderService = itemBuilderService;
        }

        [UsedImplicitly]
        public ExchangePacketController()
        {

        }

        [UsedImplicitly]
        public void ExchangeList(ExcListPacket packet)
        {
            if (packet.Gold > Session.Character.Gold || packet.BankGold > Session.Account.BankMoney)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_GOLD));
                return;
            }

            var list = new ServerExcListPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.VisualId,
                Gold = packet.Gold,
                BankGold = packet.BankGold,
                SubPackets = new List<ServerExcListSubPacket>()
            };
            
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == _exchangeService.GetTargetId(Session.Character.VisualId) && s.MapInstanceId == Session.Character.MapInstanceId) as Character;

            if (packet.SubPackets.Count > 0 && target != null)
            {
                byte i = 0;
                foreach (var value in packet.SubPackets)
                {
                    var item = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(value.Slot, value.PocketType);

                    if (item == null || item.Amount < value.Amount)
                    {
                        var closeExchange = _exchangeService.CloseExchange(Session.Character.VisualId, ExchangeCloseType.Failure);
                        Session.SendPacket(closeExchange);
                        target.SendPacket(closeExchange);
                        return;
                    }

                    if (!item.Item.IsTradable)
                    {
                        Session.SendPacket(_exchangeService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Failure));
                        target.SendPacket(_exchangeService.CloseExchange(target.VisualId, ExchangeCloseType.Failure));
                        return;
                    }

                    _exchangeService.AddItems(Session.Character.CharacterId, item, value.Amount);

                    var subPacket = new ServerExcListSubPacket
                    {
                        ExchangeSlot = i,
                        PocketType = value.PocketType,
                        ItemVnum = item.ItemVNum,
                        Upgrade = item.Upgrade,
                        AmountOrRare = value.PocketType == PocketType.Equipment ? item.Rare : value.Amount
                    };


                    list.SubPackets.Add(subPacket);
                    i++;
                }
            }
            else
            {
                list.SubPackets.Add(new ServerExcListSubPacket { ExchangeSlot = null });
            }

            _exchangeService.SetGold(Session.Character.CharacterId, packet.Gold, packet.BankGold);
            target?.SendPacket(list);
        }

        [UsedImplicitly]
        public void RequestExchange(ExchangeRequestPacket packet)
        {
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == packet.VisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;
            ExcClosePacket closeExchange;

            if (target == null && (packet.RequestType == RequestExchangeType.Requested || packet.RequestType == RequestExchangeType.List || packet.RequestType == RequestExchangeType.Cancelled))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER));
                return;
            }

            if (Session.Character.InShop || (target?.InShop ?? false))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.PLAYER_IN_SHOP));
                return;
            }

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    _exchangeService.RequestExchange(Session, target.Session);
                    return;

                case RequestExchangeType.List:
                    if (!_exchangeService.OpenExchange(Session.Character.VisualId, target.CharacterId))
                    {
                        return;
                    }
                    Session.SendPacket(new ServerExcListPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = target.VisualId,
                        Gold = null
                    });

                    target.SendPacket(new ServerExcListPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = Session.Character.VisualId,
                        Gold = null
                    });
                    return;

                case RequestExchangeType.Declined:
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED, Session.Account.Language), SayColorType.Yellow));
                    target?.SendPacket(target.GenerateSay(target.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED), SayColorType.Yellow));
                    return;

                case RequestExchangeType.Confirmed:
                    var targetId = _exchangeService.GetTargetId(Session.Character.CharacterId);

                    if (!targetId.HasValue)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.INVALID_EXCHANGE));
                        return;
                    }
                    
                    target = Broadcaster.Instance.GetCharacter(s => s.VisualId == targetId.Value && s.MapInstance == Session.Character.MapInstance) as Character;

                    if (target == null)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER));
                        return;
                    }

                    var success = true;
                    _exchangeService.ConfirmExchange(Session.Character.VisualId);

                    if (!_exchangeService.IsExchangeConfirmed(Session.Character.VisualId) || !_exchangeService.IsExchangeConfirmed(target.VisualId))
                    {
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.IN_WAITING_FOR, Session.Account.Language) });
                        return;
                    }

                    var exchangeInfo = _exchangeService.GetData(Session.Character.CharacterId);
                    var targetInfo = _exchangeService.GetData(target.CharacterId);

                    if (exchangeInfo.Gold + target.Gold > _worldConfiguration.MaxGoldAmount)
                    {
                        success = false;
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD, target.Account.Language)});
                    }

                    if (success && targetInfo.Gold + Session.Character.Gold > _worldConfiguration.MaxGoldAmount)
                    {
                        success = false;
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD, Session.Account.Language) });
                    }

                    if (success && exchangeInfo.BankGold + target.Account.BankMoney > _worldConfiguration.MaxBankGoldAmount)
                    {
                        success = false;
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, Session.Account.Language) });
                    }

                    if (success && targetInfo.BankGold + Session.Account.BankMoney > _worldConfiguration.MaxBankGoldAmount)
                    {
                        success = false;
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, Session.Account.Language) });
                    }

                    if (success && exchangeInfo.ExchangeItems.Keys.Any(s => !s.Item.IsTradable))
                    {
                        success = false;
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_TRADABLE, Session.Account.Language) });
                    }

                    if (success && !Session.Character.Inventory.EnoughPlace(targetInfo.ExchangeItems.Keys.ToList()) || !target.Inventory.EnoughPlace(exchangeInfo.ExchangeItems.Keys.ToList()))
                    {
                        success = false;
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, Session.Account.Language) });
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, target.Account.Language) });
                    }

                    if (success)
                    {
                        //TODO: fix this
                        //var itemList = _exchangeService.ProcessExchange(new Tuple<long, long>(Session.Character.VisualId, target.VisualId), Session.Character.Inventory, target.Inventory);

                        //foreach (var item in itemList)
                        //{
                        //    var sessionItem = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(item.Slot, item.Type);
                        //    var targetItem = target.Inventory.LoadBySlotAndType<IItemInstance>(item.Slot, item.Type);
                        //    Session.SendPacket(sessionItem.GeneratePocketChange(sessionItem.Type, sessionItem.Slot));
                        //    target.SendPacket(targetItem.GeneratePocketChange(targetItem.Type, targetItem.Slot));
                        //}

                        var getData = _exchangeService.GetData(Session.Character.CharacterId);
                        Session.Character.Gold -= getData.Gold;
                        Session.Account.BankMoney -= getData.BankGold * 1000;
                        Session.SendPacket(Session.Character.GenerateGold());

                        target.Gold -= getData.Gold;
                        target.Account.BankMoney -= getData.BankGold * 1000;
                        target.SendPacket(target.GenerateGold());
                    }

                    closeExchange = _exchangeService.CloseExchange(Session.Character.VisualId, success ? ExchangeCloseType.Success : ExchangeCloseType.Failure);
                    target?.SendPacket(closeExchange);
                    Session.SendPacket(closeExchange);
                    return;

                case RequestExchangeType.Cancelled:
                    if (!_exchangeService.CheckExchange(Session.Character.CharacterId, target.VisualId))
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.USER_NOT_IN_EXCHANGE));
                        return;
                    }

                    closeExchange = _exchangeService.CloseExchange(Session.Character.VisualId, ExchangeCloseType.Success);
                    target?.SendPacket(closeExchange);
                    Session.SendPacket(closeExchange);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

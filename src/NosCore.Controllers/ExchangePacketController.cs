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
using System.Collections.Generic;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using Serilog;

namespace NosCore.Controllers
{
    public class ExchangePacketController : PacketController
    {
        private readonly IExchangeProvider _exchangeProvider;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly WorldConfiguration _worldConfiguration;

        public ExchangePacketController(WorldConfiguration worldConfiguration, IExchangeProvider exchangeProvider)
        {
            _worldConfiguration = worldConfiguration;
            _exchangeProvider = exchangeProvider;
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
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NOT_ENOUGH_GOLD));
                return;
            }

            var subPacketList = new List<ServerExcListSubPacket>();

            var target = Broadcaster.Instance.GetCharacter(s =>
                s.VisualId == _exchangeProvider.GetTargetId(Session.Character.VisualId) &&
                s.MapInstanceId == Session.Character.MapInstanceId) as Character;

            if (packet.SubPackets.Count > 0 && target != null)
            {
                byte i = 0;
                foreach (var value in packet.SubPackets)
                {
                    var item = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(value.Slot,
                        value.PocketType);

                    if (item == null || item.Amount < value.Amount)
                    {
                        var closeExchange =
                            _exchangeProvider.CloseExchange(Session.Character.VisualId, ExchangeResultType.Failure);
                        Session.SendPacket(closeExchange);
                        target.SendPacket(closeExchange);
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE_LIST));
                        return;
                    }

                    if (!item.Item.IsTradable)
                    {
                        Session.SendPacket(_exchangeProvider.CloseExchange(Session.Character.CharacterId,
                            ExchangeResultType.Failure));
                        target.SendPacket(_exchangeProvider.CloseExchange(target.VisualId, ExchangeResultType.Failure));
                        _logger.Error(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANNOT_TRADE_NOT_TRADABLE_ITEM));
                        return;
                    }

                    _exchangeProvider.AddItems(Session.Character.CharacterId, item, value.Amount);

                    var subPacket = new ServerExcListSubPacket
                    {
                        ExchangeSlot = i,
                        PocketType = value.PocketType,
                        ItemVnum = item.ItemVNum,
                        Upgrade = item.Upgrade,
                        AmountOrRare = value.PocketType == PocketType.Equipment ? item.Rare : value.Amount
                    };


                    subPacketList.Add(subPacket);
                    i++;
                }
            }
            else
            {
                subPacketList.Add(new ServerExcListSubPacket {ExchangeSlot = null});
            }

            _exchangeProvider.SetGold(Session.Character.CharacterId, packet.Gold, packet.BankGold);
            target?.SendPacket(
                Session.Character.GenerateServerExcListPacket(packet.Gold, packet.BankGold, subPacketList));
        }

        [UsedImplicitly]
        public void RequestExchange(ExchangeRequestPacket packet)
        {
            var target = Broadcaster.Instance.GetCharacter(s =>
                s.VisualId == packet.VisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;
            ExcClosePacket closeExchange;

            if (target != null && (packet.RequestType == RequestExchangeType.Confirmed ||
                packet.RequestType == RequestExchangeType.Cancelled))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_FIND_CHARACTER));
                return;
            }

            if (Session.Character.InShop || (target?.InShop ?? false))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PLAYER_IN_SHOP));
                return;
            }

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (_exchangeProvider.CheckExchange(Session.Character.CharacterId) ||
                        _exchangeProvider.CheckExchange(target.VisualId))
                    {
                        Session.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_EXCHANGE,
                                Session.Account.Language),
                            Type = MessageType.White
                        });
                        return;
                    }

                    if (target.ExchangeBlocked)
                    {
                        Session.SendPacket(Session.Character.GenerateSay(
                            Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_BLOCKED, Session.Account.Language),
                            SayColorType.Purple));
                        return;
                    }

                    if (Session.Character.IsRelatedToCharacter(target.VisualId, CharacterRelationType.Blocked))
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                Session.Account.Language)
                        });
                        return;
                    }

                    if (Session.Character.InShop || target.InShop)
                    {
                        Session.SendPacket(new MsgPacket
                        {
                            Message =
                                Language.Instance.GetMessageFromKey(LanguageKey.HAS_SHOP_OPENED,
                                    Session.Account.Language),
                            Type = MessageType.White
                        });
                        return;
                    }

                    Session.SendPacket(new ModalPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.YOU_ASK_FOR_EXCHANGE,
                            Session.Account.Language),
                        Type = 0
                    });

                    target.SendPacket(new DlgPacket
                    {
                        YesPacket = new ExchangeRequestPacket
                            {RequestType = RequestExchangeType.List, VisualId = Session.Character.VisualId},
                        NoPacket = new ExchangeRequestPacket
                            {RequestType = RequestExchangeType.Declined, VisualId = Session.Character.VisualId},
                        Question = Language.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE,
                            Session.Account.Language)
                    });
                    return;

                case RequestExchangeType.List:
                    if (!_exchangeProvider.OpenExchange(Session.Character.VisualId, target.CharacterId))
                    {
                        return;
                    }

                    Session.SendPacket(Session.Character.GenerateServerExcListPacket(null, null, null));
                    target.SendPacket(target.GenerateServerExcListPacket(null, null, null));
                    return;

                case RequestExchangeType.Declined:
                    Session.SendPacket(Session.Character.GenerateSay(
                        Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED, Session.Account.Language),
                        SayColorType.Yellow));
                    target?.SendPacket(target.GenerateSay(target.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED),
                        SayColorType.Yellow));
                    return;

                case RequestExchangeType.Confirmed:
                    var targetId = _exchangeProvider.GetTargetId(Session.Character.CharacterId);

                    if (!targetId.HasValue)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE));
                        return;
                    }

                    var exchangeTarget = Broadcaster.Instance.GetCharacter(s =>
                        s.VisualId == targetId.Value && s.MapInstance == Session.Character.MapInstance);

                    if (exchangeTarget == null)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_FIND_CHARACTER));
                        return;
                    }

                    _exchangeProvider.ConfirmExchange(Session.Character.VisualId);

                    if (!_exchangeProvider.IsExchangeConfirmed(Session.Character.VisualId) ||
                        !_exchangeProvider.IsExchangeConfirmed(exchangeTarget.VisualId))
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.IN_WAITING_FOR,
                                Session.Account.Language)
                        });
                        return;
                    }

                    var success = _exchangeProvider.ValidateExchange(Session, exchangeTarget);

                    if (success.Item1 == ExchangeResultType.Success)
                    {
                        foreach (var infoPacket in success.Item2)
                        {
                            if (infoPacket.Key == Session.Character.CharacterId)
                            {
                                Session.SendPacket(infoPacket.Value);
                            }
                            else if (infoPacket.Key == exchangeTarget.VisualId)
                            {
                                exchangeTarget.SendPacket(infoPacket.Value);
                            }
                            else
                            {
                                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE));
                            }
                        }
                    }
                    else
                    {
                        var itemList = _exchangeProvider.ProcessExchange(Session.Character.VisualId,
                            exchangeTarget.VisualId, Session.Character.Inventory, exchangeTarget.Inventory);

                        foreach (var item in itemList)
                        {
                            if (item.Key == Session.Character.CharacterId)
                            {
                                Session.SendPacket(item.Value);
                            }
                            else
                            {
                                exchangeTarget.SendPacket(item.Value);
                            }
                        }

                        var getSessionData = _exchangeProvider.GetData(Session.Character.CharacterId);
                        Session.Character.RemoveGold(getSessionData.Gold);
                        Session.Character.RemoveBankGold(getSessionData.BankGold * 1000);

                        exchangeTarget.AddGold(getSessionData.Gold);
                        exchangeTarget.AddBankGold(getSessionData.BankGold * 1000);

                        var getTargetData = _exchangeProvider.GetData(exchangeTarget.VisualId);
                        exchangeTarget.RemoveGold(getTargetData.Gold);
                        exchangeTarget.RemoveBankGold(getTargetData.BankGold * 1000);

                        Session.Character.AddGold(getTargetData.Gold);
                        Session.Character.AddBankGold(getTargetData.BankGold * 1000);
                    }

                    closeExchange = _exchangeProvider.CloseExchange(Session.Character.VisualId, success.Item1);
                    exchangeTarget?.SendPacket(closeExchange);
                    Session.SendPacket(closeExchange);
                    return;

                case RequestExchangeType.Cancelled:
                    var cancelId = _exchangeProvider.GetTargetId(Session.Character.CharacterId);
                    if (!cancelId.HasValue)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.USER_NOT_IN_EXCHANGE));
                        return;
                    }

                    var cancelTarget = Broadcaster.Instance.GetCharacter(s => s.VisualId == cancelId.Value);

                    closeExchange =
                        _exchangeProvider.CloseExchange(Session.Character.VisualId, ExchangeResultType.Failure);
                    cancelTarget?.SendPacket(closeExchange);
                    Session.SendPacket(closeExchange);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
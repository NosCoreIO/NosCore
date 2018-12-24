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
using NosCore.GameObject.Services.ExchangeInfo;
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
        private readonly ExchangeAccessService _exchangeAccessService;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        
        public ExchangePacketController(WorldConfiguration worldConfiguration, ExchangeAccessService exchangeAccessService)
        {
            _worldConfiguration = worldConfiguration;
            _exchangeAccessService = exchangeAccessService;
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
                return;
            }

            var list = new ServerExcListPacket
            {
                SenderType = SenderType.Server,
                VisualId = Session.Character.VisualId,
                Gold = packet.Gold,
                BankGold = packet.BankGold,
                SubPackets = new List<ServerExcListSubPacket>()
            };
            
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == Session.Character.ExchangeData.TargetVisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;


            if (packet.SubPackets.Any())
            {
                byte i = 0;
                foreach (var value in packet.SubPackets)
                {
                    if (value.PocketType == PocketType.Bazaar)
                    {
                        return;
                    }

                    var item = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(value.Slot, value.PocketType);

                    if (item == null || item.Amount <= 0 || item.Amount < value.Amount)
                    {
                        return;
                    }

                    if (!item.Item.IsTradable)
                    {
                        _exchangeAccessService.CloseExchange(Session, target?.Session, ExchangeCloseType.Failure);
                        return;
                    }

                    var itemCpy = (IItemInstance)item.Clone();
                    itemCpy.Amount = value.Amount;
                    Session.Character.ExchangeData.ExchangeItems.TryAdd(Session.Character.ExchangeData.TargetVisualId, itemCpy);

                    var subPacket = new ServerExcListSubPacket
                    {
                        ExchangeSlot = i,
                        PocketType = value.PocketType,
                        ItemVnum = itemCpy.ItemVNum
                    };

                    if (value.PocketType == PocketType.Equipment)
                    {
                        subPacket.Amount = itemCpy.Rare;
                        subPacket.Upgrade = itemCpy.Upgrade;
                    }
                    else
                    {
                        subPacket.Amount = itemCpy.Amount;
                    }

                    list.SubPackets.Add(subPacket);
                    i++;
                }
            }
            else
            {
                list.SubPackets.Add(new ServerExcListSubPacket { ExchangeSlot = -1 });
            }

            Session.Character.ExchangeData.Gold = packet.Gold;
            Session.Character.ExchangeData.BankGold = packet.BankGold;
            target?.SendPacket(list);
            Session.Character.ExchangeData.ExchangeListIsValid = true;
        }

        [UsedImplicitly]
        public void RequestExchange(ExchangeRequestPacket packet)
        {
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == packet.VisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (target == null)
                    {
                        return;
                    }

                    _exchangeAccessService.RequestExchange(Session, target.Session);
                    break;

                case RequestExchangeType.List:
                    if (target == null || target.InExchangeOrShop)
                    {
                        return;
                    }
                    
                    _exchangeAccessService.OpenExchange(Session, target.Session);
                    break;
                case RequestExchangeType.Declined:
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED, Session.Account.Language), SayColorType.Yellow));
                    target?.SendPacket(target.GenerateSay(target.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED), SayColorType.Yellow));
                    break;
                case RequestExchangeType.Confirmed:
                    target = Broadcaster.Instance.GetCharacter(s => s.VisualId == Session.Character.ExchangeData.TargetVisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;

                    if (target == null)
                    {
                        _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER, Session.Account.Language));
                        return;
                    }

                    if (!Session.Character.ExchangeData.ExchangeListIsValid || !target.ExchangeData.ExchangeListIsValid)
                    {
                        return;
                    }

                    Session.Character.ExchangeData.ExchangeConfirmed = true;

                    if (!Session.Character.ExchangeData.ExchangeConfirmed || !target.ExchangeData.ExchangeConfirmed)
                    {
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.IN_WAITING_FOR, Session.Account.Language) });
                        return;
                    }

                    var exchangeInfo = Session.Character.ExchangeData;
                    var targetInfo = target.ExchangeData;
                    
                    if (exchangeInfo.Gold + target.Gold > _worldConfiguration.MaxGoldAmount)
                    {
                        _exchangeAccessService.CloseExchange(Session, target.Session, ExchangeCloseType.Failure);
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD, target.Account.Language)});
                        return;
                    }

                    if (targetInfo.Gold + Session.Character.Gold > _worldConfiguration.MaxGoldAmount)
                    {

                        _exchangeAccessService.CloseExchange(Session, target.Session, ExchangeCloseType.Failure);
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD, Session.Account.Language) });
                        return;
                    }

                    if (exchangeInfo.BankGold + target.Account.BankMoney > _worldConfiguration.MaxBankGoldAmount)
                    {
                        _exchangeAccessService.CloseExchange(Session, target.Session, ExchangeCloseType.Failure);
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, Session.Account.Language) });
                        return;
                    }

                    if (targetInfo.BankGold + Session.Account.BankMoney > _worldConfiguration.MaxBankGoldAmount)
                    {
                        _exchangeAccessService.CloseExchange(Session, target.Session, ExchangeCloseType.Failure);
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, Session.Account.Language) });
                        return;
                    }

                    if (Session.Character.ExchangeData.ExchangeItems.Values.Any(s => !s.Item.IsTradable))
                    {
                        _exchangeAccessService.CloseExchange(Session, target.Session, ExchangeCloseType.Failure);
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_TRADABLE, Session.Account.Language) });
                        return;
                    }

                    if (!Session.Character.Inventory.EnoughPlace(targetInfo.ExchangeItems.Values.ToList()) || !target.Inventory.EnoughPlace(exchangeInfo.ExchangeItems.Values.ToList()))
                    {
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, Session.Account.Language) });
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, target.Account.Language) });
                        _exchangeAccessService.CloseExchange(Session, target.Session, ExchangeCloseType.Failure);
                        return;
                    }

                    _exchangeAccessService.ProcessExchange(Session, target.Session);
                    _exchangeAccessService.ProcessExchange(target.Session, Session);
                    _exchangeAccessService.CloseExchange(Session, target.Session, ExchangeCloseType.Success);
                    break;
                case RequestExchangeType.Cancelled:
                    target = Broadcaster.Instance.GetCharacter(s => s.VisualId == Session.Character.ExchangeData.TargetVisualId) as Character;
                    _exchangeAccessService.CloseExchange(Session, target?.Session, ExchangeCloseType.Failure);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

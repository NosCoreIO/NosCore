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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.Inventory;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeService
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IItemBuilderService _itemBuilderService;

        public ExchangeService()
        {
            _exchangeDatas = new ConcurrentDictionary<long, ExchangeData>();
            _exchangeRequests = new ConcurrentDictionary<long, long>();
        }

        [UsedImplicitly]
        public ExchangeService(IItemBuilderService itemBuilderService)
        {
            _exchangeDatas = new ConcurrentDictionary<long, ExchangeData>();
            _exchangeRequests = new ConcurrentDictionary<long, long>();
            _itemBuilderService = itemBuilderService;
        }

        private ConcurrentDictionary<long, ExchangeData> _exchangeDatas;

        private ConcurrentDictionary<long, long> _exchangeRequests;

        public void SetGold(long visualId, long gold, long bankGold)
        {
            _exchangeDatas[visualId].Gold = gold;
            _exchangeDatas[visualId].BankGold = bankGold;
        }

        public void ConfirmExchange(long visualId)
        {
            _exchangeDatas[visualId].ExchangeConfirmed = true;
        }

        public bool IsExchangeConfirmed(long visualId)
        {
            return _exchangeDatas[visualId].ExchangeConfirmed;
        }

        public ExchangeData GetData(long visualId)
        {
            return _exchangeDatas[visualId];
        }

        public void AddItems(long visualId, IItemInstance item, short amount)
        {
            var data = _exchangeRequests.FirstOrDefault(k => k.Key == visualId || k.Value == visualId);
            if (data.Equals(default))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.INVALID_EXCHANGE));
                return;
            }

            _exchangeDatas[data.Key].ExchangeItems.TryAdd(item, amount);
        }

        public bool CheckExchange(long visualId)
        {
            return _exchangeRequests.Any(k => k.Key == visualId || k.Value == visualId);
        }

        public long? GetTargetId(long visualId)
        {
            var id = _exchangeRequests.FirstOrDefault(k => k.Key == visualId || k.Value == visualId);
            if (id.Equals(default))
            {
                return null;

            }

            return id.Value == visualId ? id.Key : id.Value;
        }

        public bool CheckExchange(long visualId, long targetId)
        {
            return _exchangeRequests.Any(k => k.Key == visualId && k.Value == visualId) ||
                _exchangeRequests.Any(k => k.Key == targetId && k.Value == targetId);
        }

        public ExcClosePacket CloseExchange(long visualId, ExchangeCloseType closeType)
        {
            var data = _exchangeRequests.FirstOrDefault(k => k.Key == visualId || k.Value == visualId);
            if (data.Equals(default))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.INVALID_EXCHANGE));
                return null;
            }
            _exchangeDatas.TryRemove(data.Key, out _);

            return new ExcClosePacket
            {
                Type = closeType
            };
        }



        public bool OpenExchange(long visualId, long targetVisualId)
        {
            if (CheckExchange(visualId) || CheckExchange(targetVisualId))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ALREADY_EXCHANGE));
                return false;
            }

            _exchangeDatas[visualId] = new ExchangeData();
            _exchangeDatas[targetVisualId] = new ExchangeData();
            return true;
        }

        //TODO MOVE TO CONTROLLER
        public void RequestExchange(ClientSession session, ClientSession targetSession)
        {
            if (targetSession.Character.InExchangeOrShop || session.Character.InExchangeOrShop)
            {
                session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_EXCHANGE,
                        session.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            if (targetSession.Character.ExchangeBlocked)
            {
                session.SendPacket(session.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_BLOCKED, session.Account.Language),
                    SayColorType.Purple));
                return;
            }

            if (session.Character.IsRelatedToCharacter(targetSession.Character.VisualId, CharacterRelationType.Blocked))
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                        session.Account.Language)
                });
                return;
            }

            if (session.Character.InShop || targetSession.Character.InShop)
            {
                session.SendPacket(new MsgPacket
                {
                    Message =
                        Language.Instance.GetMessageFromKey(LanguageKey.HAS_SHOP_OPENED, session.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            session.SendPacket(new ModalPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.YOU_ASK_FOR_EXCHANGE,
                    session.Account.Language),
                Type = 0
            });

            _exchangeRequests[session.Character.CharacterId] = targetSession.Character.CharacterId;
            targetSession.Character.SendPacket(new DlgPacket
            {
                YesPacket = new ExchangeRequestPacket
                { RequestType = RequestExchangeType.List, VisualId = session.Character.VisualId },
                NoPacket = new ExchangeRequestPacket
                { RequestType = RequestExchangeType.Declined, VisualId = session.Character.VisualId },
                Question = Language.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE, session.Account.Language)
            });
        }

        public List<IItemInstance> ProcessExchange(Tuple<long, long> users, IInventoryService sessionInventory, IInventoryService targetInventory)
        {
            var usersArray = new[] { users.Item1, users.Item2 };
            var items = new List<IItemInstance>();

            foreach (var user in usersArray)
            {
                foreach (var item in _exchangeDatas[user].ExchangeItems)
                {
                    var destInventory = user == users.Item1 ? sessionInventory : targetInventory;
                    var origintory = user == users.Item2 ? sessionInventory : targetInventory;
                    if (item.Value == item.Key.Amount)
                    {
                        origintory.Remove(item.Key.Id);
                        items.AddRange(destInventory.AddItemToPocket(item.Key));
                    }
                    else
                    {
                        origintory.RemoveItemAmountFromInventory(item.Value, item.Key.Id);
                        items.AddRange(destInventory.AddItemToPocket(_itemBuilderService.Create(item.Key.ItemVNum, user)));
                    }
                }
            }

            return items;

        }
    }
}
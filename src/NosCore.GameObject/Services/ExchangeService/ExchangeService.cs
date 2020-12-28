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
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeService : IExchangeService
    {
        private readonly IItemGenerationService _itemBuilderService;
        private readonly ILogger _logger;
        private readonly IOptions<WorldConfiguration> _worldConfiguration;
        private readonly ExchangeRequestHolder _requestHolder;

        public ExchangeService(IItemGenerationService itemBuilderService, IOptions<WorldConfiguration> worldConfiguration, ILogger logger, ExchangeRequestHolder requestHolder)
        {
            _itemBuilderService = itemBuilderService;
            _worldConfiguration = worldConfiguration;
            _logger = logger;
            _requestHolder = requestHolder;
        }

        public void SetGold(long visualId, long gold, long bankGold)
        {
            _requestHolder.ExchangeDatas[visualId].Gold = gold;
            _requestHolder.ExchangeDatas[visualId].BankGold = bankGold;
        }

        //TODO: Remove these clientsessions as parameter
        public Tuple<ExchangeResultType, Dictionary<long, IPacket>?> ValidateExchange(ClientSession session,
            ICharacterEntity targetSession)
        {
            var exchangeInfo = GetData(session.Character.CharacterId);
            var targetInfo = GetData(targetSession.VisualId);
            var dictionary = new Dictionary<long, IPacket>();

            if (exchangeInfo.Gold + targetSession.Gold > _worldConfiguration.Value.MaxGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = Game18NConstString.FullInventory
                });
            }

            if (targetInfo.Gold + session.Character.Gold > _worldConfiguration.Value.MaxGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = Game18NConstString.MaxGoldReached
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (exchangeInfo.BankGold + targetSession.BankGold > _worldConfiguration.Value.MaxBankGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, session.Account.Language)
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (targetInfo.BankGold + session.Account.BankMoney > _worldConfiguration.Value.MaxBankGoldAmount)
            {
                dictionary.Add(session.Character.CharacterId, new InfoPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, session.Account.Language)
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (exchangeInfo.ExchangeItems.Keys.Any(s => !s.ItemInstance!.Item!.IsTradable))
            {
                dictionary.Add(session.Character.CharacterId, new InfoiPacket
                {
                    Message = Game18NConstString.ItemCanNotBeSold
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (session.Character.InventoryService.EnoughPlace(
                targetInfo.ExchangeItems.Keys.Select(s => s.ItemInstance!).ToList(),
                targetInfo.ExchangeItems.Keys.First().Type) && targetSession.InventoryService.EnoughPlace(
                exchangeInfo.ExchangeItems.Keys.Select(s => s.ItemInstance!).ToList(),
                targetInfo.ExchangeItems.Keys.First().Type))
            {
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Success, null);
            }

            dictionary.Add(session.Character.CharacterId, new InfoiPacket
            {
                Message = Game18NConstString.FullInventory
            });
            dictionary.Add(targetSession.VisualId, new InfoiPacket
            {
                Message = Game18NConstString.FullInventory
            });
            return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                dictionary);

        }

        public void ConfirmExchange(long visualId)
        {
            _requestHolder.ExchangeDatas[visualId].ExchangeConfirmed = true;
        }

        public bool IsExchangeConfirmed(long visualId)
        {
            return _requestHolder.ExchangeDatas[visualId].ExchangeConfirmed;
        }

        public ExchangeData GetData(long visualId)
        {
            return _requestHolder.ExchangeDatas[visualId];
        }

        public void AddItems(long visualId, InventoryItemInstance item, short amount)
        {
            var data = _requestHolder.ExchangeRequests.FirstOrDefault(k => k.Key == visualId);
            if (data.Equals(default))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE));
                return;
            }

            _requestHolder.ExchangeDatas[data.Key].ExchangeItems.TryAdd(item, amount);
        }

        public bool CheckExchange(long visualId)
        {
            return _requestHolder.ExchangeRequests.Any(k => (k.Key == visualId) || (k.Value == visualId));
        }

        public long? GetTargetId(long visualId)
        {
            var id = _requestHolder.ExchangeRequests.FirstOrDefault(k => (k.Key == visualId) || (k.Value == visualId));
            if (id.Equals(default(KeyValuePair<long, long>)))
            {
                return null;
            }

            return id.Value == visualId ? id.Key : id.Value;
        }

        public bool CheckExchange(long visualId, long targetId)
        {
            return _requestHolder.ExchangeRequests.Any(k => (k.Key == visualId) && (k.Value == visualId)) ||
                _requestHolder.ExchangeRequests.Any(k => (k.Key == targetId) && (k.Value == targetId));
        }

        public ExcClosePacket? CloseExchange(long visualId, ExchangeResultType resultType)
        {
            var data = _requestHolder.ExchangeRequests.FirstOrDefault(k => (k.Key == visualId) || (k.Value == visualId));
            if ((data.Key == 0) && (data.Value == 0))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE));
                return null;
            }

            if (!_requestHolder.ExchangeDatas.TryRemove(data.Key, out _) || !_requestHolder.ExchangeRequests.TryRemove(data.Key, out _))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.TRY_REMOVE_FAILED), data.Key);
            }

            if (!_requestHolder.ExchangeDatas.TryRemove(data.Value, out _) || !_requestHolder.ExchangeRequests.TryRemove(data.Value, out _))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.TRY_REMOVE_FAILED), data.Value);
            }

            return new ExcClosePacket
            {
                Type = resultType
            };
        }

        public bool OpenExchange(long visualId, long targetVisualId)
        {
            if (CheckExchange(visualId) || CheckExchange(targetVisualId))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ALREADY_EXCHANGE));
                return false;
            }

            _requestHolder.ExchangeRequests[visualId] = targetVisualId;
            _requestHolder.ExchangeRequests[targetVisualId] = visualId;
            _requestHolder.ExchangeDatas[visualId] = new ExchangeData();
            _requestHolder.ExchangeDatas[targetVisualId] = new ExchangeData();
            return true;
        }

        public List<KeyValuePair<long, IvnPacket>> ProcessExchange(long firstUser, long secondUser,
            IInventoryService sessionInventory, IInventoryService targetInventory)
        {
            var usersArray = new[] { firstUser, secondUser };
            var items = new List<KeyValuePair<long, IvnPacket>>(); //SessionId, PocketChange

            foreach (var user in usersArray)
            {
                foreach (var item in _requestHolder.ExchangeDatas[user].ExchangeItems)
                {
                    var destInventory = user == firstUser ? targetInventory : sessionInventory;
                    var originInventory = user == firstUser ? sessionInventory : targetInventory;
                    var targetId = user == firstUser ? secondUser : firstUser;
                    var sessionId = user == firstUser ? firstUser : secondUser;
                    InventoryItemInstance? newItem = null;

                    if (item.Value == item.Key.ItemInstance!.Amount)
                    {
                        originInventory.Remove(item.Key.ItemInstanceId);
                    }
                    else
                    {
                        newItem = originInventory.RemoveItemAmountFromInventory(item.Value, item.Key.ItemInstanceId);
                    }

                    var inv = destInventory.AddItemToPocket(InventoryItemInstance.Create(_itemBuilderService.Create(
                        item.Key.ItemInstance.ItemVNum,
                        item.Key.ItemInstance.Amount, (sbyte)item.Key.ItemInstance.Rare, item.Key.ItemInstance.Upgrade,
                        (byte)item.Key.ItemInstance.Design), targetId))?.FirstOrDefault();
                    if (inv == null)
                    {
                        return items;
                    }

                    items.Add(new KeyValuePair<long, IvnPacket>(sessionId,
                        newItem.GeneratePocketChange((PocketType)item.Key.Type, item.Key.Slot)));
                    items.Add(new KeyValuePair<long, IvnPacket>(targetId,
                        item.Key.GeneratePocketChange((PocketType)inv.Type, inv.Slot)));
                }
            }

            return items;
        }
    }
}
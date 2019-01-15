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
using NosCore.Configuration;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
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
        private readonly WorldConfiguration _worldConfiguration;

        public ExchangeService()
        {
            _exchangeDatas = new ConcurrentDictionary<long, ExchangeData>();
            _exchangeRequests = new ConcurrentDictionary<long, long>();
        }

        [UsedImplicitly]
        public ExchangeService(IItemBuilderService itemBuilderService, WorldConfiguration worldConfiguration)
        {
            _exchangeDatas = new ConcurrentDictionary<long, ExchangeData>();
            _exchangeRequests = new ConcurrentDictionary<long, long>();
            _itemBuilderService = itemBuilderService;
            _worldConfiguration = worldConfiguration;
        }

        private readonly ConcurrentDictionary<long, ExchangeData> _exchangeDatas;

        private readonly ConcurrentDictionary<long, long> _exchangeRequests;

        public void SetGold(long visualId, long gold, long bankGold)
        {
            _exchangeDatas[visualId].Gold = gold;
            _exchangeDatas[visualId].BankGold = bankGold;
        }

        //TODO: Remove these clientsessions as parameter
        public Tuple<ExchangeResultType, Dictionary<long, InfoPacket>> ValidateExchange(ClientSession session, ICharacterEntity targetSession)
        {
            var exchangeInfo = GetData(session.Character.CharacterId);
            var targetInfo = GetData(targetSession.VisualId);
            var dictionary = new Dictionary<long, InfoPacket>();

            if (exchangeInfo.Gold + targetSession.Gold > _worldConfiguration.MaxGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, targetSession.AccountLanguage)
                });
            }

            if (targetInfo.Gold + session.Character.Gold > _worldConfiguration.MaxGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD, session.Account.Language)
                });
                return new Tuple<ExchangeResultType, Dictionary<long, InfoPacket>>(ExchangeResultType.Failure, dictionary);
            }

            if (exchangeInfo.BankGold + targetSession.BankGold > _worldConfiguration.MaxBankGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, session.Account.Language)
                });
                return new Tuple<ExchangeResultType, Dictionary<long, InfoPacket>>(ExchangeResultType.Failure, dictionary);
            }

            if (targetInfo.BankGold + session.Account.BankMoney > _worldConfiguration.MaxBankGoldAmount)
            {
                dictionary.Add(session.Character.CharacterId, new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, session.Account.Language)
                });
                return new Tuple<ExchangeResultType, Dictionary<long, InfoPacket>>(ExchangeResultType.Failure, dictionary);
            }

            if (exchangeInfo.ExchangeItems.Keys.Any(s => !s.Item.IsTradable))
            {
                dictionary.Add(session.Character.CharacterId, new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_TRADABLE, session.Account.Language)
                });
                return new Tuple<ExchangeResultType, Dictionary<long, InfoPacket>>(ExchangeResultType.Failure, dictionary);
            }

            if (!session.Character.Inventory.EnoughPlace(targetInfo.ExchangeItems.Keys.ToList()) || !targetSession.Inventory.EnoughPlace(exchangeInfo.ExchangeItems.Keys.ToList()))
            {
                dictionary.Add(session.Character.CharacterId, new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, session.Account.Language)
                });
                dictionary.Add(targetSession.VisualId, new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, targetSession.AccountLanguage)
                });
                return new Tuple<ExchangeResultType, Dictionary<long, InfoPacket>>(ExchangeResultType.Failure, dictionary);
            }

            return new Tuple<ExchangeResultType, Dictionary<long, InfoPacket>>(ExchangeResultType.Success, null);
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
            var data = _exchangeRequests.FirstOrDefault(k => k.Key == visualId);
            if (data.Equals(default))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE));
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

        public ExcClosePacket CloseExchange(long visualId, ExchangeResultType resultType)
        {
            var data = _exchangeRequests.FirstOrDefault(k => k.Key == visualId || k.Value == visualId);
            if (data.Key == 0 && data.Value == 0)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE));
                return null;
            }

            if (!_exchangeDatas.TryRemove(data.Key, out _) || !_exchangeRequests.TryRemove(data.Key, out _))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.TRY_REMOVE_FAILED), data.Key);
            }

            if (!_exchangeDatas.TryRemove(data.Value, out _) || !_exchangeRequests.TryRemove(data.Value, out _))
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

            _exchangeRequests[visualId] = targetVisualId;
            _exchangeRequests[targetVisualId] = visualId;
            _exchangeDatas[visualId] = new ExchangeData();
            _exchangeDatas[targetVisualId] = new ExchangeData();
            return true;
        }

        public List<KeyValuePair<long, IvnPacket>> ProcessExchange(long firstUser, long secondUser, IInventoryService sessionInventory, IInventoryService targetInventory)
        {
            var usersArray = new[] { firstUser, secondUser };
            var items = new List<KeyValuePair<long, IvnPacket>>(); //SessionId, PocketChange

            foreach (var user in usersArray)
            {
                foreach (var item in _exchangeDatas[user].ExchangeItems)
                {
                    var destInventory = user == firstUser ? targetInventory : sessionInventory;
                    var originInventory = user == firstUser ? sessionInventory : targetInventory;
                    var targetId = user == firstUser ? secondUser : firstUser;
                    var sessionId = user == firstUser ? firstUser : secondUser;
                    IItemInstance newItem = null;

                    if (item.Value == item.Key.Amount)
                    {
                        originInventory.Remove(item.Key.Id);
                    }
                    else
                    {
                        newItem = originInventory.RemoveItemAmountFromInventory(item.Value, item.Key.Id);
                    }

                    var inv = destInventory.AddItemToPocket(_itemBuilderService.Create(item.Key.ItemVNum,
                        targetId, amount: item.Key.Amount, rare: (sbyte)item.Key.Rare, upgrade: item.Key.Upgrade, design: (byte)item.Key.Design)).FirstOrDefault();

                    items.Add(new KeyValuePair<long, IvnPacket>(sessionId, newItem.GeneratePocketChange(item.Key.Type, item.Key.Slot)));
                    items.Add(new KeyValuePair<long, IvnPacket>(targetId, item.Key.GeneratePocketChange(inv.Type, inv.Slot)));
                }
            }

            return items;

        }
    }
}
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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeService(IItemGenerationService itemBuilderService,
            IOptions<WorldConfiguration> worldConfiguration, ILogger logger, IExchangeRequestRegistry exchangeRegistry,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IGameLanguageLocalizer gameLanguageLocalizer)
        : IExchangeService
    {
        public void SetGold(long visualId, long gold, long bankGold)
        {
            var data = exchangeRegistry.GetExchangeData(visualId);
            if (data != null)
            {
                data.Gold = gold;
                data.BankGold = bankGold;
            }
        }

        public Tuple<ExchangeResultType, Dictionary<long, IPacket>?> ValidateExchange(ClientSession session,
            ICharacterEntity targetSession)
        {
            var exchangeInfo = GetData(session.Character.CharacterId);
            var targetInfo = GetData(targetSession.VisualId);
            var dictionary = new Dictionary<long, IPacket>();

            if (exchangeInfo.Gold + targetSession.Gold > worldConfiguration.Value.MaxGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = Game18NConstString.FullInventory
                });
            }

            if (targetInfo.Gold + session.Character.Gold > worldConfiguration.Value.MaxGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = Game18NConstString.MaxGoldReached
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (exchangeInfo.BankGold + targetSession.BankGold > worldConfiguration.Value.MaxBankGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoPacket
                {
                    Message = gameLanguageLocalizer[LanguageKey.BANK_FULL, session.Account.Language]
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (targetInfo.BankGold + session.Account.BankMoney > worldConfiguration.Value.MaxBankGoldAmount)
            {
                dictionary.Add(session.Character.CharacterId, new InfoPacket
                {
                    Message = gameLanguageLocalizer[LanguageKey.BANK_FULL, session.Account.Language]
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (exchangeInfo.ExchangeItems.Keys.Any(s => !s.ItemInstance.Item.IsTradable))
            {
                dictionary.Add(session.Character.CharacterId, new InfoiPacket
                {
                    Message = Game18NConstString.ItemCanNotBeSold
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (session.Character.InventoryService.EnoughPlace(
                targetInfo.ExchangeItems.Keys.Select(s => s.ItemInstance).ToList(),
                targetInfo.ExchangeItems.Keys.First().Type) && targetSession.InventoryService.EnoughPlace(
                exchangeInfo.ExchangeItems.Keys.Select(s => s.ItemInstance).ToList(),
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
            var data = exchangeRegistry.GetExchangeData(visualId);
            if (data != null)
            {
                data.ExchangeConfirmed = true;
            }
        }

        public bool IsExchangeConfirmed(long visualId)
        {
            return exchangeRegistry.GetExchangeData(visualId)?.ExchangeConfirmed ?? false;
        }

        public ExchangeData GetData(long visualId)
        {
            return exchangeRegistry.GetExchangeData(visualId) ?? new ExchangeData();
        }

        public void AddItems(long visualId, InventoryItemInstance item, short amount)
        {
            var request = exchangeRegistry.GetExchangeRequest(visualId);
            if (request == null)
            {
                logger.Error(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                return;
            }

            var data = exchangeRegistry.GetExchangeData(visualId);
            data?.ExchangeItems.TryAdd(item, amount);
        }

        public bool CheckExchange(long visualId)
        {
            return exchangeRegistry.HasExchange(visualId);
        }

        public long? GetTargetId(long visualId)
        {
            var pair = exchangeRegistry.GetExchangeRequestPair(visualId);
            if (pair == null)
            {
                return null;
            }

            return pair.Value.Value == visualId ? pair.Value.Key : pair.Value.Value;
        }

        public bool CheckExchange(long visualId, long targetId)
        {
            var pair1 = exchangeRegistry.GetExchangeRequestPair(visualId);
            var pair2 = exchangeRegistry.GetExchangeRequestPair(targetId);
            return (pair1 != null && pair1.Value.Key == visualId && pair1.Value.Value == visualId) ||
                   (pair2 != null && pair2.Value.Key == targetId && pair2.Value.Value == targetId);
        }

        public ExcClosePacket? CloseExchange(long visualId, ExchangeResultType resultType)
        {
            var pair = exchangeRegistry.GetExchangeRequestPair(visualId);
            if (pair == null)
            {
                logger.Error(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                return null;
            }

            var data = pair.Value;
            if (!exchangeRegistry.RemoveExchangeData(data.Key) || !exchangeRegistry.RemoveExchangeRequest(data.Key))
            {
                logger.Error(logLanguage[LogLanguageKey.TRY_REMOVE_FAILED], data.Key);
            }

            if (!exchangeRegistry.RemoveExchangeData(data.Value) || !exchangeRegistry.RemoveExchangeRequest(data.Value))
            {
                logger.Error(logLanguage[LogLanguageKey.TRY_REMOVE_FAILED], data.Value);
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
                logger.Error(logLanguage[LogLanguageKey.ALREADY_EXCHANGE]);
                return false;
            }

            exchangeRegistry.SetExchangeRequest(visualId, targetVisualId);
            exchangeRegistry.SetExchangeRequest(targetVisualId, visualId);
            exchangeRegistry.SetExchangeData(visualId, new ExchangeData());
            exchangeRegistry.SetExchangeData(targetVisualId, new ExchangeData());
            return true;
        }

        public List<KeyValuePair<long, IvnPacket>> ProcessExchange(long firstUser, long secondUser,
            IInventoryService sessionInventory, IInventoryService targetInventory)
        {
            var usersArray = new[] { firstUser, secondUser };
            var items = new List<KeyValuePair<long, IvnPacket>>();

            foreach (var user in usersArray)
            {
                var userData = exchangeRegistry.GetExchangeData(user);
                if (userData == null) continue;

                foreach (var item in userData.ExchangeItems)
                {
                    var destInventory = user == firstUser ? targetInventory : sessionInventory;
                    var originInventory = user == firstUser ? sessionInventory : targetInventory;
                    var targetId = user == firstUser ? secondUser : firstUser;
                    var sessionId = user == firstUser ? firstUser : secondUser;
                    InventoryItemInstance? newItem = null;

                    if (item.Value == item.Key.ItemInstance.Amount)
                    {
                        originInventory.Remove(item.Key.ItemInstanceId);
                    }
                    else
                    {
                        newItem = originInventory.RemoveItemAmountFromInventory(item.Value, item.Key.ItemInstanceId);
                    }

                    var inv = destInventory.AddItemToPocket(InventoryItemInstance.Create(itemBuilderService.Create(
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

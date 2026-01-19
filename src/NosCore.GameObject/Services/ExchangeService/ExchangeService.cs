//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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

            var targetFirstItem = targetInfo.ExchangeItems.Keys.FirstOrDefault();
            var exchangeFirstItem = exchangeInfo.ExchangeItems.Keys.FirstOrDefault();

            var targetHasItems = targetFirstItem != null;
            var exchangeHasItems = exchangeFirstItem != null;

            var targetEnoughPlace = !targetHasItems || session.Character.InventoryService.EnoughPlace(
                targetInfo.ExchangeItems.Keys.Select(s => s.ItemInstance).ToList(),
                targetFirstItem!.Type);

            var exchangeEnoughPlace = !exchangeHasItems || targetSession.InventoryService.EnoughPlace(
                exchangeInfo.ExchangeItems.Keys.Select(s => s.ItemInstance).ToList(),
                exchangeFirstItem!.Type);

            if (targetEnoughPlace && exchangeEnoughPlace)
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
            var pendingTransfers = new List<(
                IInventoryService OriginInventory,
                IInventoryService DestInventory,
                InventoryItemInstance OriginalItem,
                short Amount,
                long TargetId,
                long SessionId,
                bool IsFullTransfer
            )>();

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
                    var isFullTransfer = item.Value == item.Key.ItemInstance.Amount;

                    pendingTransfers.Add((originInventory, destInventory, item.Key, item.Value, targetId, sessionId, isFullTransfer));
                }
            }

            var addedItems = new List<(IInventoryService Inventory, InventoryItemInstance Item)>();

            foreach (var transfer in pendingTransfers)
            {
                var newItem = itemBuilderService.Create(
                    transfer.OriginalItem.ItemInstance.ItemVNum,
                    transfer.Amount,
                    (sbyte)transfer.OriginalItem.ItemInstance.Rare,
                    transfer.OriginalItem.ItemInstance.Upgrade,
                    (byte)transfer.OriginalItem.ItemInstance.Design);

                var inv = transfer.DestInventory.AddItemToPocket(
                    InventoryItemInstance.Create(newItem, transfer.TargetId))?.FirstOrDefault();

                if (inv == null)
                {
                    foreach (var added in addedItems)
                    {
                        added.Inventory.DeleteFromTypeAndSlot(added.Item.Type, added.Item.Slot);
                    }
                    return new List<KeyValuePair<long, IvnPacket>>();
                }

                addedItems.Add((transfer.DestInventory, inv));
            }

            for (var i = 0; i < pendingTransfers.Count; i++)
            {
                var transfer = pendingTransfers[i];
                var addedItem = addedItems[i];

                InventoryItemInstance? sourceItem = null;
                if (transfer.IsFullTransfer)
                {
                    transfer.OriginInventory.Remove(transfer.OriginalItem.ItemInstanceId);
                }
                else
                {
                    sourceItem = transfer.OriginInventory.RemoveItemAmountFromInventory(transfer.Amount, transfer.OriginalItem.ItemInstanceId);
                }

                items.Add(new KeyValuePair<long, IvnPacket>(transfer.SessionId,
                    (sourceItem ?? transfer.OriginalItem).GeneratePocketChange((PocketType)transfer.OriginalItem.Type, transfer.OriginalItem.Slot)));
                items.Add(new KeyValuePair<long, IvnPacket>(transfer.TargetId,
                    transfer.OriginalItem.GeneratePocketChange((PocketType)addedItem.Item.Type, addedItem.Item.Slot)));
            }

            return items;
        }
    }
}

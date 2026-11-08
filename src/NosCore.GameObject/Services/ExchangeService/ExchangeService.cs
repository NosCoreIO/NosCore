//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeService(IOptions<WorldConfiguration> worldConfiguration, ILogger<ExchangeService> logger,
            IExchangeRequestRegistry exchangeRegistry, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, IClock clock, IShellOptionService shellOptionService,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao)
        : IExchangeService
    {
        public static readonly Duration RequestLifetime = Duration.FromSeconds(30);

        public static readonly Duration RequestCooldown = Duration.FromSeconds(5);

        private readonly object _openLock = new();

        public Duration? GetRequestCooldown(PlayerComponentBundle requester)
        {
            if (requester.LastExchangeRequest is not { } last)
            {
                return null;
            }

            var remaining = last + RequestCooldown - clock.GetCurrentInstant();
            return remaining > Duration.Zero ? remaining : null;
        }

        public void RegisterRequest(PlayerComponentBundle requester, long targetId)
        {
            var now = clock.GetCurrentInstant();
            requester.ExchangeRequestCharacterIds[targetId] = now;
            requester.LastExchangeRequest = now;
        }

        public bool TakeRequest(PlayerComponentBundle requester, long targetId)
        {
            return requester.ExchangeRequestCharacterIds.TryRemove(targetId, out var requestedAt)
                && clock.GetCurrentInstant() - requestedAt <= RequestLifetime;
        }

        public bool SetOffer(long visualId, long gold, long bankGold,
            IReadOnlyDictionary<InventoryItemInstance, short> items)
        {
            var data = exchangeRegistry.GetExchangeData(visualId);
            var targetId = GetTargetId(visualId);
            var partner = targetId.HasValue ? exchangeRegistry.GetExchangeData(targetId.Value) : null;
            if (data == null || partner == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                return false;
            }

            lock (data.Settlement)
            {
                if (data.Settlement.IsClaimed)
                {
                    return false;
                }

                data.ExchangeItems = new ConcurrentDictionary<InventoryItemInstance, short>(items);
                data.Gold = gold;
                data.BankGold = bankGold;
                data.ExchangeConfirmed = false;
                partner.ExchangeConfirmed = false;
                return true;
            }
        }

        public Tuple<ExchangeResultType, Dictionary<long, IPacket>?> ValidateExchange(ClientSession session,
            ICharacterEntity targetSession)
        {
            var exchangeInfo = GetData(session.Character.CharacterId);
            var targetInfo = GetData(targetSession.VisualId);
            var dictionary = new Dictionary<long, IPacket>();

            if (exchangeInfo.Gold > session.Character.Gold || exchangeInfo.BankGold > session.Character.BankGold)
            {
                dictionary.Add(session.Character.CharacterId, new InfoiPacket
                {
                    Message = exchangeInfo.Gold > session.Character.Gold
                        ? Game18NConstString.NotEnoughGold
                        : Game18NConstString.NotEnoughFounds
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (targetInfo.Gold > targetSession.Gold || targetInfo.BankGold > targetSession.BankGold)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = targetInfo.Gold > targetSession.Gold
                        ? Game18NConstString.NotEnoughGold
                        : Game18NConstString.NotEnoughFounds
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

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
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = Game18NConstString.CanNotDepositAnymore
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (targetInfo.BankGold + session.Character.BankGold > worldConfiguration.Value.MaxBankGoldAmount)
            {
                dictionary.Add(session.Character.CharacterId, new InfoiPacket
                {
                    Message = Game18NConstString.CanNotDepositAnymore
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

        public bool ConfirmExchange(long visualId)
        {
            var data = exchangeRegistry.GetExchangeData(visualId);
            var targetId = GetTargetId(visualId);
            var partner = targetId.HasValue ? exchangeRegistry.GetExchangeData(targetId.Value) : null;
            if (data == null || partner == null)
            {
                return false;
            }

            lock (data.Settlement)
            {
                if (data.Settlement.IsClaimed)
                {
                    return false;
                }

                data.ExchangeConfirmed = true;
                if (!partner.ExchangeConfirmed)
                {
                    return false;
                }

                data.Settlement.IsClaimed = true;
                return true;
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
            KeyValuePair<long, long> data;
            lock (_openLock)
            {
                var pair = exchangeRegistry.GetExchangeRequestPair(visualId);
                if (pair == null)
                {
                    logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                    return null;
                }

                data = pair.Value;
                var settlement = exchangeRegistry.GetExchangeData(data.Key)?.Settlement;
                if (settlement != null)
                {
                    lock (settlement)
                    {
                        settlement.IsClaimed = true;
                    }
                }

                if (!exchangeRegistry.RemoveExchangeData(data.Key) || !exchangeRegistry.RemoveExchangeRequest(data.Key))
                {
                    logger.LogError(logLanguage[LogLanguageKey.TRY_REMOVE_FAILED], data.Key);
                }

                if (!exchangeRegistry.RemoveExchangeData(data.Value) || !exchangeRegistry.RemoveExchangeRequest(data.Value))
                {
                    logger.LogError(logLanguage[LogLanguageKey.TRY_REMOVE_FAILED], data.Value);
                }
            }

            SetInExchange(data.Key, false);
            SetInExchange(data.Value, false);

            return new ExcClosePacket
            {
                Type = resultType
            };
        }

        // The lifecycle owns the flag. Set at the seven call sites instead, it was simply
        // never set at all, which left every InExchangeOrShop guard inert.
        private void SetInExchange(long visualId, bool value)
        {
            if (sessionRegistry.TryGetCharacter(c => c.VisualId == visualId, out var character))
            {
                character.InExchange = value;
            }
        }

        public bool OpenExchange(long visualId, long targetVisualId)
        {
            lock (_openLock)
            {
                if (CheckExchange(visualId) || CheckExchange(targetVisualId))
                {
                    logger.LogError(logLanguage[LogLanguageKey.ALREADY_EXCHANGE]);
                    return false;
                }

                var settlement = new ExchangeSettlement();
                exchangeRegistry.SetExchangeRequest(visualId, targetVisualId);
                exchangeRegistry.SetExchangeData(visualId, new ExchangeData { Settlement = settlement });
                exchangeRegistry.SetExchangeData(targetVisualId, new ExchangeData { Settlement = settlement });
            }

            SetInExchange(visualId, true);
            SetInExchange(targetVisualId, true);
            return true;
        }

        public async Task<List<KeyValuePair<long, IvnPacket>>?> ProcessExchangeAsync(long firstUser, long secondUser,
            IInventoryService sessionInventory, IInventoryService targetInventory)
        {
            var transfers = new List<(
                IInventoryService OriginInventory,
                IInventoryService DestInventory,
                InventoryItemInstance OriginalItem,
                short Amount,
                long TargetId,
                long SessionId
            )>();

            foreach (var user in new[] { firstUser, secondUser })
            {
                var userData = exchangeRegistry.GetExchangeData(user);
                if (userData == null)
                {
                    logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                    return null;
                }

                var origin = user == firstUser ? sessionInventory : targetInventory;
                var dest = user == firstUser ? targetInventory : sessionInventory;
                var targetId = user == firstUser ? secondUser : firstUser;
                foreach (var item in userData.ExchangeItems)
                {
                    transfers.Add((origin, dest, item.Key, item.Value, targetId, user));
                }
            }

            // The offer holds references taken when the item was put in the window: each one must
            // still be there, at least at the offered amount.
            foreach (var transfer in transfers)
            {
                if (transfer.Amount <= 0
                    || !transfer.OriginInventory.TryGetValue(transfer.OriginalItem.ItemInstanceId, out var live)
                    || !ReferenceEquals(live, transfer.OriginalItem)
                    || live.ItemInstance == null
                    || live.ItemInstance.Amount < transfer.Amount)
                {
                    logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                    return null;
                }
            }

            var options = transfers
                .Where(t => t.OriginalItem.ItemInstance is WearableInstance)
                .ToDictionary(t => t.OriginalItem.ItemInstanceId,
                    t => shellOptionService.Load(t.OriginalItem.ItemInstanceId));

            var snapshots = new[] { Snapshot(sessionInventory), Snapshot(targetInventory) };
            var removals = new List<KeyValuePair<long, IvnPacket>>();
            var additions = new List<KeyValuePair<long, IvnPacket>>();
            var clones = new List<(Guid SourceId, IItemInstance Clone)>();

            foreach (var transfer in transfers)
            {
                var original = transfer.OriginalItem;
                InventoryItemInstance? remaining = null;
                if (transfer.Amount == original.ItemInstance.Amount)
                {
                    transfer.OriginInventory.Remove(original.ItemInstanceId);
                }
                else
                {
                    remaining = transfer.OriginInventory.RemoveItemAmountFromInventory(transfer.Amount, original.ItemInstanceId);
                }

                removals.Add(new KeyValuePair<long, IvnPacket>(transfer.SessionId,
                    remaining.GeneratePocketChange((PocketType)original.Type, original.Slot)));
            }

            foreach (var transfer in transfers)
            {
                var clone = CloneForReceiver(transfer.OriginalItem.ItemInstance, transfer.Amount);
                var before = transfer.DestInventory.Values
                    .Where(s => s.ItemInstance.ItemVNum == clone.ItemVNum)
                    .ToDictionary(s => s.ItemInstanceId, s => s.ItemInstance.Amount);
                var touched = transfer.DestInventory.AddItemToPocket(
                    InventoryItemInstance.Create(clone, transfer.TargetId)) ?? [];
                var received = touched.Sum(s =>
                    s.ItemInstance.Amount - (before.TryGetValue(s.ItemInstanceId, out var was) ? was : 0));

                if (received != transfer.Amount)
                {
                    foreach (var snapshot in snapshots)
                    {
                        Restore(snapshot);
                    }

                    logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                    return null;
                }

                clones.Add((transfer.OriginalItem.ItemInstanceId, clone));
                additions.AddRange(touched.Select(s => new KeyValuePair<long, IvnPacket>(transfer.TargetId,
                    s.GeneratePocketChange((PocketType)s.Type, s.Slot))));
            }

            foreach (var (sourceId, clone) in clones)
            {
                if (clone is not WearableInstance || !options.TryGetValue(sourceId, out var cloneOptions)
                    || cloneOptions.Count == 0)
                {
                    continue;
                }

                await itemInstanceDao.TryInsertOrUpdateAsync(clone);
                await shellOptionService.SaveAsync(clone.Id, cloneOptions);
            }

            return removals.Concat(additions).ToList();
        }

        private static IItemInstance CloneForReceiver(IItemInstance source, short amount)
        {
            var clone = (IItemInstance)source.Clone();
            clone.Id = Guid.NewGuid();
            clone.Amount = amount;
            if (clone is WearableInstance { EquipmentOptions: { } equipmentOptions } wearable)
            {
                wearable.EquipmentOptions = equipmentOptions.Select(o => new EquipmentOptionDto
                {
                    Id = Guid.NewGuid(),
                    WearableInstanceId = clone.Id,
                    Level = o.Level,
                    Type = o.Type,
                    Value = o.Value
                }).ToList();
            }

            return clone;
        }

        private static (IInventoryService Inventory, List<(InventoryItemInstance Item, short Amount)> Items) Snapshot(
            IInventoryService inventory) =>
            (inventory, inventory.Values.Select(s => (s, s.ItemInstance.Amount)).ToList());

        private static void Restore((IInventoryService Inventory, List<(InventoryItemInstance Item, short Amount)> Items) snapshot)
        {
            var kept = snapshot.Items.Select(s => s.Item.ItemInstanceId).ToHashSet();
            foreach (var added in snapshot.Inventory.Keys.Where(id => !kept.Contains(id)).ToList())
            {
                snapshot.Inventory.Remove(added);
            }

            foreach (var (item, amount) in snapshot.Items)
            {
                item.ItemInstance.Amount = amount;
                snapshot.Inventory[item.ItemInstanceId] = item;
            }
        }
    }
}

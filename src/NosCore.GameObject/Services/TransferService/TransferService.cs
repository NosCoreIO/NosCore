//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransferService
{
    public class TransferService(IOptions<WorldConfiguration> worldConfiguration, ILogger<TransferService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IDaoTransactionScope daoTransactionScope,
            IDao<CharacterDto, long> characterDao, IDao<AccountDto, long> accountDao,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao,
            IDao<EquipmentOptionDto, Guid> equipmentOptionDao)
        : ITransferService
    {
        private sealed record Side(PlayerComponentBundle Giver, TransferOffer Offer, PlayerComponentBundle Receiver);

        private sealed class Changes
        {
            public List<KeyValuePair<long, IvnPacket>> Packets { get; } = new();
            public List<InventoryItemInstance> Upserted { get; } = new();
            public List<InventoryItemInstance> Deleted { get; } = new();
            public List<EquipmentOptionDto> Options { get; } = new();
        }

        public async Task<List<KeyValuePair<long, IvnPacket>>?> TransferAsync(PlayerComponentBundle first,
            TransferOffer firstOffer, PlayerComponentBundle second, TransferOffer secondOffer)
        {
            var sides = new[] { new Side(first, firstOffer, second), new Side(second, secondOffer, first) };
            if (!sides.All(IsCovered))
            {
                logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                return null;
            }

            var options = sides.SelectMany(s => s.Offer.Items.Keys)
                .Where(i => i.ItemInstance is WearableInstance)
                .ToDictionary(i => i.ItemInstanceId,
                    i => equipmentOptionDao.Where(o => o.WearableInstanceId == i.ItemInstanceId)?.ToList() ?? []);

            var snapshot = new Snapshot(first, second);
            var changes = Apply(sides, options);
            if (changes == null || !await PersistAsync(first, second, changes))
            {
                snapshot.Restore();
                logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                return null;
            }

            return changes.Packets;
        }

        private bool IsCovered(Side side)
        {
            var offer = side.Offer;
            var config = worldConfiguration.Value;
            if (offer.Gold < 0 || offer.BankGold < 0 || offer.Gold > side.Giver.Gold || offer.BankGold > side.Giver.BankGold
                || side.Receiver.Gold + offer.Gold > config.MaxGoldAmount
                || side.Receiver.BankGold + offer.BankGold > config.MaxBankGoldAmount)
            {
                return false;
            }

            return offer.Items.All(item => item.Value > 0
                && side.Giver.InventoryService.TryGetValue(item.Key.ItemInstanceId, out var live)
                && ReferenceEquals(live, item.Key)
                && live.ItemInstance.Amount >= item.Value);
        }

        private static Changes? Apply(IReadOnlyList<Side> sides, IReadOnlyDictionary<Guid, List<EquipmentOptionDto>> options)
        {
            var changes = new Changes();
            foreach (var side in sides)
            {
                var giver = side.Giver;
                var receiver = side.Receiver;
                giver.Gold -= side.Offer.Gold;
                receiver.Gold += side.Offer.Gold;
                giver.BankGold -= side.Offer.BankGold;
                receiver.BankGold += side.Offer.BankGold;

                foreach (var (item, amount) in side.Offer.Items)
                {
                    InventoryItemInstance? remaining = null;
                    if (amount == item.ItemInstance.Amount)
                    {
                        side.Giver.InventoryService.Remove(item.ItemInstanceId);
                        changes.Deleted.Add(item);
                    }
                    else
                    {
                        remaining = side.Giver.InventoryService.RemoveItemAmountFromInventory(amount, item.ItemInstanceId);
                        changes.Upserted.Add(item);
                    }

                    changes.Packets.Add(new KeyValuePair<long, IvnPacket>(side.Giver.VisualId,
                        remaining.GeneratePocketChange((PocketType)item.Type, item.Slot)));
                }
            }

            foreach (var side in sides)
            {
                foreach (var (item, amount) in side.Offer.Items)
                {
                    var clone = CloneForReceiver(item.ItemInstance, amount);
                    var inventory = side.Receiver.InventoryService;
                    var before = inventory.Values
                        .Where(s => s.ItemInstance.ItemVNum == clone.ItemVNum)
                        .ToDictionary(s => s.ItemInstanceId, s => s.ItemInstance.Amount);
                    var touched = inventory.AddItemToPocket(InventoryItemInstance.Create(clone, side.Receiver.VisualId)) ?? [];
                    var received = touched.Sum(s =>
                        s.ItemInstance.Amount - (before.TryGetValue(s.ItemInstanceId, out var was) ? was : 0));
                    if (received != amount)
                    {
                        return null;
                    }

                    if (options.TryGetValue(item.ItemInstanceId, out var itemOptions))
                    {
                        changes.Options.AddRange(itemOptions.Select(o => new EquipmentOptionDto
                        {
                            Id = Guid.NewGuid(),
                            WearableInstanceId = clone.Id,
                            Level = o.Level,
                            Type = o.Type,
                            Value = o.Value
                        }));
                    }

                    changes.Upserted.AddRange(touched);
                    changes.Packets.AddRange(touched.Select(s => new KeyValuePair<long, IvnPacket>(side.Receiver.VisualId,
                        s.GeneratePocketChange((PocketType)s.Type, s.Slot))));
                }
            }

            return changes;
        }

        private async Task<bool> PersistAsync(PlayerComponentBundle first, PlayerComponentBundle second, Changes changes)
        {
            try
            {
                await using var transaction = daoTransactionScope.Begin();
                foreach (var character in new[] { first, second })
                {
                    character.CharacterDto.Gold = character.Gold;
                    if (await characterDao.TryInsertOrUpdateAsync(character.CharacterDto) == null
                        || await accountDao.TryInsertOrUpdateAsync(character.Account) == null)
                    {
                        return false;
                    }
                }

                var upserted = changes.Upserted.DistinctBy(s => s.Id).ToList();
                var written = (upserted.Count == 0
                        || (await itemInstanceDao.TryInsertOrUpdateAsync(upserted.Select(s => (IItemInstanceDto?)s.ItemInstance).ToList())
                            && await inventoryItemInstanceDao.TryInsertOrUpdateAsync(upserted.Cast<InventoryItemInstanceDto>().ToList())))
                    && (changes.Options.Count == 0 || await equipmentOptionDao.TryInsertOrUpdateAsync(changes.Options))
                    && (changes.Deleted.Count == 0
                        || (await inventoryItemInstanceDao.TryDeleteAsync(changes.Deleted.Select(s => s.Id).ToList()) != null
                            && await itemInstanceDao.TryDeleteAsync(changes.Deleted.Select(s => s.ItemInstanceId).ToList()) != null));
                if (!written)
                {
                    return false;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                return false;
            }
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

        private sealed class Snapshot
        {
            private readonly List<(PlayerComponentBundle Character, long Gold, long BankGold,
                List<(InventoryItemInstance Item, short Amount)> Items)> _parties;

            public Snapshot(params PlayerComponentBundle[] characters)
            {
                _parties = characters.Select(c => (c, c.Gold, c.BankGold,
                    c.InventoryService.Values.Select(s => (s, s.ItemInstance.Amount)).ToList())).ToList();
            }

            public void Restore()
            {
                foreach (var (character, gold, bankGold, items) in _parties)
                {
                    character.Gold = gold;
                    character.BankGold = bankGold;
                    character.CharacterDto.Gold = gold;
                    var inventory = character.InventoryService;
                    var kept = items.Select(s => s.Item.ItemInstanceId).ToHashSet();
                    foreach (var added in inventory.Keys.Where(id => !kept.Contains(id)).ToList())
                    {
                        inventory.Remove(added);
                    }

                    foreach (var (item, amount) in items)
                    {
                        item.ItemInstance.Amount = amount;
                        inventory[item.ItemInstanceId] = item;
                    }
                }
            }
        }
    }
}

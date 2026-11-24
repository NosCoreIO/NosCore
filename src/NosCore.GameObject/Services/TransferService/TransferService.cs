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
using System.Data.Common;
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
            public List<Side> Gold { get; } = new();
            public List<MovedItem> Moved { get; } = new();
        }

        private sealed record MovedItem(Side Side, InventoryItemInstance Item, short VNum, short Amount)
        {
            public IReadOnlyCollection<PlacedStack> Placed { get; set; } = [];
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

            var changes = new Changes();
            try
            {
                if (Apply(sides, options, changes) && await PersistAsync(first, second, changes))
                {
                    return changes.Packets;
                }
            }
            catch
            {
                Undo(changes);
                throw;
            }

            Undo(changes);
            logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
            return null;
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

        private static bool Apply(IReadOnlyList<Side> sides, IReadOnlyDictionary<Guid, List<EquipmentOptionDto>> options,
            Changes changes)
        {
            foreach (var side in sides)
            {
                var giver = side.Giver;
                var receiver = side.Receiver;
                giver.Gold -= side.Offer.Gold;
                receiver.Gold += side.Offer.Gold;
                giver.BankGold -= side.Offer.BankGold;
                receiver.BankGold += side.Offer.BankGold;
                changes.Gold.Add(side);

                foreach (var (item, amount) in side.Offer.Items)
                {
                    var whole = amount == item.ItemInstance.Amount;
                    if (!giver.InventoryService.TryConsume(item, amount))
                    {
                        return false;
                    }

                    var moved = new MovedItem(side, item, item.ItemInstance.ItemVNum, amount);
                    changes.Moved.Add(moved);
                    (whole ? changes.Deleted : changes.Upserted).Add(item);
                    var remaining = giver.InventoryService.TryGetValue(item.ItemInstanceId, out var left) ? left : null;
                    changes.Packets.Add(new KeyValuePair<long, IvnPacket>(giver.VisualId,
                        remaining.GeneratePocketChange(item.Type.ToPocketType(), item.Slot)));
                }
            }

            foreach (var moved in changes.Moved.ToList())
            {
                var (side, item, _, amount) = moved;
                var clone = CloneForReceiver(item.ItemInstance, amount);
                var placed = side.Receiver.InventoryService.PlaceItem(InventoryItemInstance.Create(clone, side.Receiver.VisualId)) ?? [];
                moved.Placed = placed;
                if (placed.Sum(p => p.Added) != amount)
                {
                    return false;
                }

                if (options.TryGetValue(item.ItemInstanceId, out var itemOptions))
                {
                    changes.Options.AddRange(itemOptions.Select(o => o.CopyFor(clone.Id)));
                }

                changes.Upserted.AddRange(placed.Select(p => p.Stack));
                changes.Packets.AddRange(placed.Select(p => new KeyValuePair<long, IvnPacket>(side.Receiver.VisualId,
                    p.Stack.GeneratePocketChange(p.Stack.Type.ToPocketType(), p.Stack.Slot))));
            }

            return true;
        }

        // Undoes only what this exchange moved, so anything either character gained or spent meanwhile stays. What a
        // receiver has already used is not taken back, and the giver is not paid back for it either.
        private void Undo(Changes changes)
        {
            var returnable = new List<MovedItem>();
            foreach (var moved in Enumerable.Reverse(changes.Moved))
            {
                if (moved.Side.Receiver.InventoryService.TryUndoPlacement(moved.Placed))
                {
                    returnable.Add(moved);
                    continue;
                }

                logger.LogError(logLanguage[LogLanguageKey.EXCHANGE_ITEM_NOT_RETURNED], moved.VNum, moved.Amount,
                    moved.Side.Receiver.CharacterId, moved.Side.Giver.CharacterId);
            }

            foreach (var moved in returnable)
            {
                moved.Side.Giver.InventoryService.RestoreConsumed(moved.Item, moved.Amount);
            }

            foreach (var side in Enumerable.Reverse(changes.Gold))
            {
                var (giver, offer, receiver) = side;
                var gold = Math.Min(offer.Gold, Math.Max(0, receiver.Gold));
                receiver.Gold -= gold;
                giver.Gold += gold;
                if (gold != offer.Gold)
                {
                    logger.LogError(logLanguage[LogLanguageKey.EXCHANGE_GOLD_NOT_RETURNED], gold, offer.Gold,
                        receiver.CharacterId, giver.CharacterId);
                }

                var bankGold = Math.Min(offer.BankGold, Math.Max(0, receiver.BankGold));
                receiver.BankGold -= bankGold;
                giver.BankGold += bankGold;
                if (bankGold != offer.BankGold)
                {
                    logger.LogError(logLanguage[LogLanguageKey.EXCHANGE_BANK_GOLD_NOT_RETURNED], bankGold, offer.BankGold,
                        receiver.CharacterId, giver.CharacterId);
                }

                foreach (var character in new[] { giver, receiver })
                {
                    character.CharacterDto.Gold = character.Gold;
                    character.Account.BankMoney = character.BankGold;
                }
            }
        }

        private async Task<bool> PersistAsync(PlayerComponentBundle first, PlayerComponentBundle second, Changes changes)
        {
            try
            {
                await using var transaction = daoTransactionScope.Begin();
                foreach (var character in new[] { first, second })
                {
                    character.CharacterDto.Gold = character.Gold;
                    character.Account.BankMoney = character.BankGold;
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
            catch (DbException e)
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
                wearable.EquipmentOptions = equipmentOptions.Select(o => o.CopyFor(clone.Id)).ToList();
            }

            return clone;
        }
    }
}

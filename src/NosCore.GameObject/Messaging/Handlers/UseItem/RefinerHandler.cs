//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // Each roll group of a refiner is its own draw; a drawn row with no amount gives nothing.
    [UsedImplicitly]
    public sealed class RefinerHandler(IItemCatalog itemCatalog, IItemGenerationService itemProvider,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao, IRandomProvider random)
    {
        private const byte AmountArgument = 4;
        private const byte ItemArgument = 1;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var refiner = evt.InventoryItem;
            if (refiner.ItemInstance.Item is not { ItemType: ItemType.Main, Effect: ItemEffectType.Refiner })
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var inventory = character.InventoryService;
            var gillionVNum = itemCatalog.GetMaterialVNum(MaterialType.GillionStone);
            var stone = inventory.Values
                .Where(i => i.ItemInstance.ItemVNum == gillionVNum)
                .OrderBy(i => i.Type)
                .ThenBy(i => i.Slot)
                .FirstOrDefault();
            if (stone == null)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughGillion));
                return;
            }

            var drawn = Draw(refiner.ItemInstance.ItemVNum);
            if (drawn.Count == 0)
            {
                return;
            }

            var rewards = drawn.Select(r => itemProvider.Create(r.ItemGeneratedVNum, r.ItemGeneratedAmount)).ToList();
            if (!inventory.HasRoomFor(rewards))
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                return;
            }

            if (!inventory.TryConsume(refiner, 1))
            {
                return;
            }

            var added = inventory.ConsumeAndAdd(stone, 1,
                rewards.Select(item => InventoryItemInstance.Create(item, character.CharacterId)).ToList());
            if (added is not { Count: > 0 })
            {
                var (restored, _) = inventory.RestoreConsumed(refiner, 1);
                foreach (var back in restored)
                {
                    await session.SendPacketAsync(back.GeneratePocketChange(back.Type.ToPocketType(), back.Slot));
                }

                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                return;
            }

            await session.SendPacketAsync(inventory.LoadBySlotAndType(refiner.Slot, refiner.Type)
                .GeneratePocketChange(refiner.Type.ToPocketType(), refiner.Slot));
            await session.SendPacketAsync(inventory.LoadBySlotAndType(stone.Slot, stone.Type)
                .GeneratePocketChange(stone.Type.ToPocketType(), stone.Slot));
            foreach (var inv in added)
            {
                await session.SendPacketAsync(inv.GeneratePocketChange(inv.Type.ToPocketType(), inv.Slot));
            }

            var cellaVNum = itemCatalog.GetMaterialVNum(MaterialType.CellaPowder);
            foreach (var row in drawn)
            {
                await session.SendPacketAsync(row.ItemGeneratedVNum == cellaVNum
                    ? character.GenerateSayi(SayColorType.Green, Game18NConstString.CellaPowderItemAcquired,
                        AmountArgument, (int)row.ItemGeneratedAmount)
                    : character.GenerateSayi(SayColorType.Green, Game18NConstString.SpecialItemAcquired,
                        ItemArgument, (int)row.ItemGeneratedVNum));
            }
        }

        private List<RollGeneratedItemDto> Draw(short refinerVNum) =>
            (rollGeneratedItemDao.Where(r => r.OriginalItemVNum == refinerVNum && r.Probability > 0) ?? [])
                .GroupBy(r => r.RollGroup)
                .OrderBy(g => g.Key)
                .Select(g => Pick(g.OrderBy(r => r.RollGeneratedItemId).ToList()))
                .Where(r => r.ItemGeneratedAmount > 0)
                .ToList();

        private RollGeneratedItemDto Pick(List<RollGeneratedItemDto> rows)
        {
            var roll = random.Next(0, rows.Sum(r => (int)r.Probability));
            return rows.First(r => (roll -= r.Probability) < 0);
        }
    }
}

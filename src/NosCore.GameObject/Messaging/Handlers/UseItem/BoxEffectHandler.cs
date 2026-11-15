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
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class BoxEffectHandler(
        IItemGenerationService itemProvider,
        IItemCatalog itemCatalog,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao, IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            var effect = source.ItemInstance.Item.Effect;
            if (effect is not (ItemEffectType.BoxEffect or ItemEffectType.ClassBox))
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var boxVNum = source.ItemInstance.ItemVNum;
            var boxRare = source.ItemInstance.Rare;
            var boxDesign = source.ItemInstance.Design;

            var sets = (rollGeneratedItemDao.Where(r =>
                    r.OriginalItemVNum == boxVNum
                    && r.MinimumOriginalItemRare <= boxRare
                    && r.MaximumOriginalItemRare >= boxRare
                    && r.OriginalItemDesign == boxDesign
                    && r.Probability > 0)
                ?? Enumerable.Empty<RollGeneratedItemDto>())
                .GroupBy(r => r.RollGroup)
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(r => r.ItemGeneratedVNum).ToList())
                .ToList();

            if (effect == ItemEffectType.ClassBox)
            {
                var fitting = sets.Where(set => set.All(r => itemCatalog.GetItem(r.ItemGeneratedVNum) is { } item
                    && item.FitsClass(character.Class) && item.FitsGender(character.Gender))).ToList();
                if (fitting.Count > 0)
                {
                    sets = fitting;
                }
            }

            var sum = sets.Sum(set => (int)set[0].Probability);
            if (sum <= 0)
            {
                return;
            }

            var roll = random.Next(0, sum);
            var cumulative = 0;
            List<RollGeneratedItemDto>? chosen = null;
            foreach (var set in sets)
            {
                cumulative += set[0].Probability;
                if (roll < cumulative)
                {
                    chosen = set;
                    break;
                }
            }
            if (chosen == null) return;

            var rewards = chosen.Select(r => itemProvider.Create(
                r.ItemGeneratedVNum,
                r.ItemGeneratedAmount,
                r.IsRareRandom ? (sbyte)boxRare : (sbyte)0,
                r.ItemGeneratedUpgrade)).OfType<IItemInstance>().ToList();
            if (rewards.Count != chosen.Count) return;

            var inventory = character.InventoryService;
            if (!inventory.HasRoomFor(rewards))
            {
                await SendNotEnoughSpaceAsync(evt);
                return;
            }

            if (!inventory.TryConsume(source, 1))
            {
                return;
            }

            var added = rewards.SelectMany(reward => inventory.AddItemToPocket(InventoryItemInstance.Create(reward, character.CharacterId))
                ?? []).ToList();
            if (added.Count == 0)
            {
                inventory.RestoreConsumed(source, 1);
                await SendNotEnoughSpaceAsync(evt);
                return;
            }

            foreach (var inv in added)
            {
                await session.SendPacketAsync(inv.GeneratePocketChange(inv.Type.ToPocketType(), inv.Slot));
            }
            foreach (var row in chosen)
            {
                await session.SendPacketAsync(new RdiPacket
                {
                    ItemVNum = row.ItemGeneratedVNum,
                    Amount = row.ItemGeneratedAmount,
                });
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, row.ItemGeneratedVNum.ToString(), row.ItemGeneratedAmount));
            }

            await session.SendPacketAsync(inventory.LoadBySlotAndType(source.Slot, source.Type)
                .GeneratePocketChange(source.Type.ToPocketType(), source.Slot));
        }

        private static Task SendNotEnoughSpaceAsync(ItemUsedEvent evt) =>
            evt.ClientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = evt.ClientSession.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.NotEnoughSpace
            });
    }
}

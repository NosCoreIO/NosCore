//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class BoxEffectHandler(
        IItemGenerationService itemProvider,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            if (source.ItemInstance.Item.Effect != ItemEffectType.BoxEffect)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var boxVNum = source.ItemInstance.ItemVNum;
            var boxRare = source.ItemInstance.Rare;
            var boxDesign = source.ItemInstance.Design;

            var pool = (rollGeneratedItemDao.Where(r =>
                    r.OriginalItemVNum == boxVNum
                    && r.MinimumOriginalItemRare <= boxRare
                    && r.MaximumOriginalItemRare >= boxRare
                    && r.OriginalItemDesign == boxDesign)
                ?? Enumerable.Empty<RollGeneratedItemDto>())
                .ToList();

            if (pool.Count == 0)
            {
                return;
            }

            var sum = pool.Sum(r => (int)r.Probability);
            if (sum <= 0)
            {
                return;
            }

            var roll = RandomHelper.Instance.RandomNumber(0, sum);
            var cumulative = 0;
            RollGeneratedItemDto? chosen = null;
            foreach (var entry in pool)
            {
                cumulative += entry.Probability;
                if (roll < cumulative)
                {
                    chosen = entry;
                    break;
                }
            }
            chosen ??= pool.Last();

            var reward = itemProvider.Create(
                chosen.ItemGeneratedVNum,
                chosen.ItemGeneratedAmount,
                chosen.IsRareRandom ? (sbyte)boxRare : (sbyte)0,
                chosen.ItemGeneratedUpgrade);
            var added = character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(reward, character.CharacterId))?.FirstOrDefault();
            if (added == null)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.NotEnoughSpace
                });
                return;
            }

            await session.SendPacketAsync(added.GeneratePocketChange((PocketType)added.Type, added.Slot));
            await session.SendPacketAsync(new RdiPacket
            {
                ItemVNum = chosen.ItemGeneratedVNum,
                Amount = chosen.ItemGeneratedAmount,
            });
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.ItemReceived,
                ArgumentType = 2,
                Game18NArguments = { chosen.ItemGeneratedVNum.ToString(), chosen.ItemGeneratedAmount }
            });

            character.InventoryService.RemoveItemAmountFromInventory(1, source.ItemInstanceId);
            await session.SendPacketAsync(source.GeneratePocketChange((PocketType)source.Type, source.Slot));
        }
    }
}

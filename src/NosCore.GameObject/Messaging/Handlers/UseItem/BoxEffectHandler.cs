//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BoxService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class BoxEffectHandler(IItemGenerationService itemProvider, IBoxRollService boxRoll)
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
            var boxRare = source.ItemInstance.Rare;
            if (boxRoll.Roll(source.ItemInstance, character) is not { } chosen)
            {
                return;
            }

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

            var added = inventory.ConsumeAndAdd(source, 1, rewards
                .Select(reward => InventoryItemInstance.Create(reward, character.CharacterId)).ToList());
            if (added == null)
            {
                return;
            }

            if (added.Count == 0)
            {
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

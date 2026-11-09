//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SealedTarotCardHandler(IItemGenerationService itemProvider)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.Effect != ItemEffectType.SealedTarotCard || item.EffectValue <= 0)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;

            var gift = itemProvider.Create((short)item.EffectValue, 1);
            if (gift == null) return;

            var inventory = character.InventoryService;
            var source = evt.InventoryItem;
            if (!inventory.HasRoomFor(gift))
            {
                await SendNotEnoughSpaceAsync(evt);
                return;
            }

            if (!inventory.TryConsume(source, 1))
            {
                return;
            }

            var added = inventory.AddItemToPocket(InventoryItemInstance.Create(gift, character.CharacterId))?.FirstOrDefault();
            if (added == null)
            {
                inventory.RestoreConsumed(source, 1);
                await SendNotEnoughSpaceAsync(evt);
                return;
            }

            await session.SendPacketAsync(added.GeneratePocketChange(added.Type.ToPocketType(), added.Slot));
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.ItemReceived,
                ArgumentType = 2,
                Game18NArguments = { gift.ItemVNum.ToString(), 1 }
            });

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

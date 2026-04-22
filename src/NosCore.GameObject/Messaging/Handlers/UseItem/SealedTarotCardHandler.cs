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

            var added = character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(gift, character.CharacterId))?.FirstOrDefault();
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
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.ItemReceived,
                ArgumentType = 2,
                Game18NArguments = { gift.ItemVNum.ToString(), 1 }
            });

            character.InventoryService.RemoveItemAmountFromInventory(1, evt.InventoryItem.ItemInstanceId);
            await session.SendPacketAsync(evt.InventoryItem.GeneratePocketChange((PocketType)evt.InventoryItem.Type, evt.InventoryItem.Slot));
        }
    }
}

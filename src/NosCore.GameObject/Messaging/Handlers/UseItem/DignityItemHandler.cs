//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class DignityItemHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Magical || item.Effect != ItemEffectType.RestoreDignity
                || DignityLevels.Maximum is not { } maximum)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;

            if (character.Dignity >= maximum)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.DignityMaximum
                });
                return;
            }

            var gained = (short)Math.Min(item.EffectValue, maximum - character.Dignity);
            var itemInstance = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));

            character.Dignity += gained;
            await session.SendPacketAsync(character.GenerateFd());
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.DignityIncreased,
                ArgumentType = 4,
                Game18NArguments = { gained }
            });
        }
    }
}

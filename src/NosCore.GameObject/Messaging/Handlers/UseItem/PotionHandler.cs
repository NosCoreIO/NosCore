//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class PotionHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Potion)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var restoresHp = item.Hp > 0 && character.Hp < character.MaxHp;
            var restoresMp = item.Mp > 0 && character.Mp < character.MaxMp;
            if (!character.IsAlive || (!restoresHp && !restoresMp))
            {
                return;
            }

            var itemInstance = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));

            character.Hp = Math.Min(character.MaxHp, character.Hp + item.Hp);
            character.Mp = Math.Min(character.MaxMp, character.Mp + item.Mp);
            await session.SendPacketAsync(character.GenerateStat());
        }
    }
}

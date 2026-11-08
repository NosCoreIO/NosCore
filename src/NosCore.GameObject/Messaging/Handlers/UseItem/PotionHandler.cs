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
using NosCore.GameObject.Services.PotionService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class PotionHandler(IPotionCooldownService potionCooldown)
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
            var itemInstance = evt.InventoryItem;
            var drunk = await session.Character.OnMapAsync(() =>
            {
                var character = session.Character;
                var restoresHp = item.Hp > 0 && character.Hp < character.MaxHp;
                var restoresMp = item.Mp > 0 && character.Mp < character.MaxMp;
                if (!character.IsAlive || (!restoresHp && !restoresMp) || !potionCooldown.IsReady(character))
                {
                    return Task.FromResult(false);
                }

                potionCooldown.Start(character);
                character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
                character.Hp = Math.Min(character.MaxHp, character.Hp + item.Hp);
                character.Mp = Math.Min(character.MaxMp, character.Mp + item.Mp);
                return Task.FromResult(true);
            });
            if (!drunk)
            {
                return;
            }

            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
            await session.SendPacketAsync(session.Character.GenerateStat());
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SealedVesselHandler(List<ItemSummonDto> itemSummons, List<NpcMonsterDto> npcMonsters,
        IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var inventoryItem = evt.InventoryItem;
            if (inventoryItem.ItemInstance.Item.Effect != ItemEffectType.SealedVessel)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (character.MapInstance.IsAct4)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.CanNotBeUsedHere));
                return;
            }

            var monsters = itemSummons
                .Where(s => s.ItemVNum == inventoryItem.ItemInstance.ItemVNum)
                .Select(s => npcMonsters.Find(m => m.NpcMonsterVNum == s.NpcMonsterVNum))
                .OfType<NpcMonsterDto>()
                .ToList();
            if (monsters.Count == 0 || !character.InventoryService.TryConsume(inventoryItem, 1))
            {
                return;
            }

            var released = monsters[random.Next(0, monsters.Count)];
            await session.SendPacketAsync(inventoryItem.GeneratePocketChange(inventoryItem.Type.ToPocketType(), inventoryItem.Slot));
            await character.OnMapAsync(async () =>
            {
                var map = character.MapInstance;
                var monster = map.SpawnMonster(released, character.PositionX, character.PositionY, true, released.IsHostile);
                await map.SendPacketAsync(monster.GenerateIn());
            });
        }
    }
}

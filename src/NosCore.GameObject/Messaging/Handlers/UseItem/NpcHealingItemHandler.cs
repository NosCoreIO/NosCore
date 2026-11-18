//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // Players and pets are not NPCs, so they are never healed. Nothing is used up when no
    // NPC in reach is hurt.
    [UsedImplicitly]
    public sealed class NpcHealingItemHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            var character = evt.ClientSession.Character;
            if (item.HealsNpcsWithin == 0 || !character.IsAlive || character.MapInstance is not { } map)
            {
                return;
            }

            var healed = await character.OnMapAsync(() =>
            {
                var restored = new List<(NpcComponentBundle Npc, int Hp)>();
                foreach (var npc in map.EnumerateNpcs())
                {
                    if (Math.Abs(npc.PositionX - character.PositionX) > item.HealsNpcsWithin
                        || Math.Abs(npc.PositionY - character.PositionY) > item.HealsNpcsWithin)
                    {
                        continue;
                    }

                    var hp = npc.RestoreHp(item.EffectValue);
                    if (hp > 0)
                    {
                        restored.Add((npc, hp));
                    }
                }

                return Task.FromResult(restored);
            });
            if (healed.Count == 0)
            {
                return;
            }

            await character.UseUpOneAsync(evt.InventoryItem);
            foreach (var (npc, hp) in healed)
            {
                await map.SendPacketAsync(new RcPacket
                {
                    VisualType = VisualType.Npc,
                    VisualId = npc.VisualId,
                    HealAmount = hp,
                    Unknown = 0,
                });
                await map.SendPacketAsync(npc.GenerateStatInfo());
            }
        }
    }
}

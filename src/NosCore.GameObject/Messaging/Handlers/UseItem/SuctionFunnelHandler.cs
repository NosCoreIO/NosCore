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
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SuctionFunnelHandler(List<ItemTargetMonsterDto> targetMonsters, IItemGenerationService itemProvider,
        IBuffService buffService, IGameEventDispatcher gameEvents)
    {
        private const int HpThresholdPercent = 20;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var funnel = evt.InventoryItem;
            var item = funnel.ItemInstance.Item;
            var character = evt.ClientSession.Character;
            if (item.Effect != ItemEffectType.SuctionFunnel || !character.IsAlive
                || evt.Packet.VisualType != VisualType.Monster
                || character.MapInstance.FindMonster(m => m.VisualId == evt.Packet.VisualId) is not { } monster
                || monster.NpcMonster is not { } npcMonster
                || !targetMonsters.Any(t => t.ItemVNum == item.VNum && t.NpcMonsterVNum == npcMonster.NpcMonsterVNum)
                || itemProvider.Create((short)item.EffectValue, 1) is not { } reward)
            {
                return;
            }

            var inventory = character.InventoryService;
            if (!inventory.HasRoomFor(reward))
            {
                await character.SendPacketAsync(Message(Game18NConstString.NotEnoughSpace));
                return;
            }

            // Claimed on the map's executor, so a hit landing at the same moment cannot kill it as well.
            var drained = await monster.OnMapAsync(async () =>
            {
                if (!monster.IsAlive || monster.Hp * 100L >= monster.MaxHp * (long)HpThresholdPercent)
                {
                    return false;
                }

                monster.Hp = 0;
                monster.IsAlive = false;
                await buffService.ClearAsync(monster);
                await monster.MapInstance.SendPacketAsync(new DiePacket
                {
                    VisualType = monster.VisualType,
                    VisualId = monster.VisualId,
                    TargetVisualType = character.VisualType,
                    TargetId = character.VisualId,
                });
                return true;
            });
            if (!drained)
            {
                if (monster.IsAlive)
                {
                    await character.SendPacketAsync(Message(Game18NConstString.CanBeUsedIfHpLessTwentyPercent));
                }

                return;
            }

            var changed = inventory.ConsumeAndAdd(funnel, 1, [InventoryItemInstance.Create(reward, character.CharacterId)]);
            if (changed?.FirstOrDefault() is { } added)
            {
                await character.SendPacketAsync(added.GeneratePocketChange(added.Type.ToPocketType(), added.Slot));
                await character.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, reward.ItemVNum.ToString(), 1));
            }

            await character.SendPacketAsync(inventory.LoadBySlotAndType(funnel.Slot, funnel.Type)
                .GeneratePocketChange(funnel.Type.ToPocketType(), funnel.Slot));
            await gameEvents.RaiseAsync(new EntityDiedEvent(monster, character));
        }

        private static MsgiPacket Message(Game18NConstString message) =>
            new() { Type = MessageType.Default, Message = message };
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class VehicleHandler(
        ITransformationService transformationService,
        IClock clock,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao,
        List<ItemDto> items,
        IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            var legacyVehicle = item.Type != NoscorePocketType.Mount && item.ItemType == ItemType.Special && item.Effect == ItemEffectType.Vehicle;
            var mount = item.Type == NoscorePocketType.Mount && item.Speed > 0 && item.Morph > 0;
            var scrollMounts = item.Type == NoscorePocketType.Mount && !mount
                ? (rollGeneratedItemDao.Where(r => r.OriginalItemVNum == item.VNum) ?? [])
                    .Select(r => items.Find(i => i.VNum == r.ItemGeneratedVNum))
                    .OfType<ItemDto>()
                    .OrderBy(i => i.VNum)
                    .ToList()
                : [];
            if (!legacyVehicle && !mount && scrollMounts.Count == 0)
            {
                return;
            }

            var session = evt.ClientSession;
            var itemInstance = evt.InventoryItem;
            var packet = evt.Packet;

            var character = session.Character;
            if (packet.Mode is 0 or 1 && !character.IsVehicled)
            {
                if (item.LevelMaximum > 0 && character.Level > item.LevelMaximum)
                {
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.LevelTooHigh
                    });
                    return;
                }

                await character.StartDelayAsync(new DelayPacket
                {
                    Type = DelayPacketType.Locomotion,
                    Delay = 3000,
                    Packet = character.GenerateUseItem(itemInstance.Type.ToPocketType(), itemInstance.Slot, 2, 0)
                }, itemInstance.ItemInstanceId, clock.GetCurrentInstant());
                return;
            }

            character = session.Character;
            if (packet.Mode == 2 && !character.IsVehicled)
            {
                if (!character.TryCompleteDelay(DelayPacketType.Locomotion, itemInstance.ItemInstanceId, clock.GetCurrentInstant()))
                {
                    return;
                }

                if (scrollMounts.Count == 0)
                {
                    await transformationService.ChangeVehicleAsync(session, item, item);
                    return;
                }

                var drawn = scrollMounts[random.Next(0, scrollMounts.Count)];
                if (item.ConsumedOnMount && !character.InventoryService.TryConsume(itemInstance, 1))
                {
                    return;
                }

                await transformationService.ChangeVehicleAsync(session, item.Speed > 0 ? item : drawn, drawn);
                if (item.ConsumedOnMount)
                {
                    await session.SendPacketAsync(character.InventoryService.LoadBySlotAndType(itemInstance.Slot, itemInstance.Type)
                        .GeneratePocketChange(itemInstance.Type.ToPocketType(), itemInstance.Slot));
                }

                return;
            }

            await transformationService.RemoveVehicleAsync(session);
        }
    }
}

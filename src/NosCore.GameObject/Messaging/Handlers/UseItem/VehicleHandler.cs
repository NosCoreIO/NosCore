//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class VehicleHandler(
        ITransformationService transformationService,
        IClock clock)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            var legacyVehicle = item.Type != NoscorePocketType.Mount && item.ItemType == ItemType.Special && item.Effect == ItemEffectType.Vehicle;
            var mount = item.Type == NoscorePocketType.Mount && item.Speed > 0 && item.Morph > 0;
            if (!legacyVehicle && !mount)
            {
                return;
            }

            var session = evt.ClientSession;
            var itemInstance = evt.InventoryItem;
            var packet = evt.Packet;

            var character = session.Character;
            if (packet.Mode is 0 or 1 && !character.IsVehicled)
            {
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

                await transformationService.ChangeVehicleAsync(session, itemInstance.ItemInstance.Item);
                return;
            }

            await transformationService.RemoveVehicleAsync(session);
        }
    }
}

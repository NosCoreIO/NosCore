//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class VehicleHandler(
        ILogger<VehicleHandler> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        ITransformationService transformationService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Special || item.Effect != ItemEffectType.Vehicle)
            {
                return;
            }

            var session = evt.ClientSession;
            var itemInstance = evt.InventoryItem;
            var packet = evt.Packet;

            var character = session.Character;
            if (character.InExchangeOrShop)
            {
                logger.LogError(logLanguage[LogLanguageKey.CANT_USE_ITEM_IN_SHOP]);
                return;
            }

            character = session.Character;
            if (packet.Mode == 1 && !character.IsVehicled)
            {
                await session.SendPacketAsync(new DelayPacket
                {
                    Type = DelayPacketType.Locomotion,
                    Delay = 3000,
                    Packet = character.GenerateUseItem((PocketType)itemInstance.Type, itemInstance.Slot, 2, 0)
                });
                return;
            }

            character = session.Character;
            if (packet.Mode == 2 && !character.IsVehicled)
            {
                await transformationService.ChangeVehicleAsync(session, itemInstance.ItemInstance.Item);
                return;
            }

            await transformationService.RemoveVehicleAsync(session);
        }
    }
}

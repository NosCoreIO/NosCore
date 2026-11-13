//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class WigDyeHandler(IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Magical || item.Effect != ItemEffectType.WigDye)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (character.IsVehicled)
            {
                return;
            }

            var wig = character.InventoryService.LoadBySlotAndType((short)EquipmentType.Hat, NoscorePocketType.Wear);
            if (wig == null || !wig.ItemInstance.Item.IsColored)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.YouAreNotWearingItem
                });
                return;
            }

            wig.ItemInstance.Design = (short)random.Next(0, item.EffectValue);
            await character.MapInstance.SendPacketAsync(character.GenerateEq());
            await session.SendPacketAsync(character.GenerateEquipment());
            await character.UseUpOneAsync(evt.InventoryItem);
        }
    }
}

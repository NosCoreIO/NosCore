//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpeakerHandler
    {
        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Magical || item.Effect != ItemEffectType.Speaker)
            {
                return Task.CompletedTask;
            }

            return evt.ClientSession.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                SecondArgument = 1,
                EntityId = evt.InventoryItem.Slot
            });
        }
    }
}

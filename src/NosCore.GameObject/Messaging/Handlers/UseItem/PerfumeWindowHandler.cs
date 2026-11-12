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
    // Opens the perfume window; the item the player puts in it comes back as a guri.
    [UsedImplicitly]
    public sealed class PerfumeWindowHandler
    {
        private const uint PerfumeWindow = 1;

        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt)
        {
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.Perfume)
            {
                return Task.CompletedTask;
            }

            return evt.ClientSession.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.PerfumAndIdentification,
                Argument = PerfumeWindow,
                EntityId = evt.ClientSession.Character.CharacterId
            });
        }
    }
}

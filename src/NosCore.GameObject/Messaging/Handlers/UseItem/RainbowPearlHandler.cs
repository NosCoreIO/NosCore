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
    // Puts the client in shell-picking mode; the shell it picks comes back as a guri.
    [UsedImplicitly]
    public sealed class RainbowPearlHandler
    {
        private const uint PickShell = 0;

        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt)
        {
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.ShellIdentification)
            {
                return Task.CompletedTask;
            }

            return evt.ClientSession.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.PerfumAndIdentification,
                Argument = PickShell,
                EntityId = evt.ClientSession.Character.CharacterId
            });
        }
    }
}

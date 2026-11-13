//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class SpeechBubbleHandler(IClock clock)
    {
        // The window the bubble opens, which the client's reply repeats.
        public const uint TextInputArgument = 4;

        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.TextInputReply || packet.Argument != TextInputArgument
                || string.IsNullOrWhiteSpace(packet.Value))
            {
                return;
            }

            var character = evt.ClientSession.Character;
            var bubble = character.InventoryService.Values.FirstOrDefault(i => i.Type == NoscorePocketType.Etc
                && i.ItemInstance.Item.ItemType == ItemType.Magical
                && i.ItemInstance.Item.Effect == ItemEffectType.SpeechBubble);
            if (bubble == null)
            {
                return;
            }

            character.SpeechBubble = packet.Value;
            character.SpeechBubbleExpiresAt = clock.GetCurrentInstant()
                + Duration.FromMinutes(bubble.ItemInstance.Item.EffectValue);
            await character.UseUpOneAsync(bubble);
            await character.SendPacketAsync(new BubbleReplyPacket { Message = packet.Value });
        }
    }
}

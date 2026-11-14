//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class SelfIntroductionHandler
    {
        // The window the item opens, which the client's reply repeats.
        public const uint TextInputArgument = 2;

        // The client's own limit, from its "Enter introduction (up to 60 characters)" prompt.
        public const int MaxLength = 60;

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
            if (character.IsVehicled)
            {
                return;
            }

            var item = character.InventoryService.Values.FirstOrDefault(i => i.Type == NoscorePocketType.Main
                && i.ItemInstance.Item.ItemType == ItemType.Special
                && i.ItemInstance.Item.Effect == ItemEffectType.SelfIntroduction);
            if (item == null)
            {
                return;
            }

            var introduction = packet.Value.Trim();
            if (introduction.Length > MaxLength)
            {
                introduction = introduction[..MaxLength].TrimEnd();
            }

            character.Biography = introduction;
            await character.UseUpOneAsync(item);
            await character.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.SelfIntroductionRegistered
            });
        }
    }
}

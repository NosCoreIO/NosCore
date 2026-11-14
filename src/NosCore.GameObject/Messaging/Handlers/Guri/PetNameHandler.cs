//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class PetNameHandler
    {
        // The window the name tag opens, which the client's reply repeats.
        public const uint TextInputArgument = 1;

        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.TextInputReply || packet.Argument != TextInputArgument
                || string.IsNullOrWhiteSpace(packet.Value) || packet.Data is not { } transportId)
            {
                return;
            }

            var owner = evt.ClientSession.Character;
            if (!owner.Mates.TryGetValue(transportId, out var mate) || mate.MateType != MateType.Pet)
            {
                return;
            }

            var tag = owner.InventoryService.Values.FirstOrDefault(i => i.Type == NoscorePocketType.Etc
                && i.ItemInstance.Item.ItemType == ItemType.Teacher
                && i.ItemInstance.Item.Effect == (ItemEffectType)TeacherItemEffectType.PetNameTag);
            if (tag == null)
            {
                return;
            }

            mate.Name = packet.Value;
            await owner.UseUpOneAsync(tag);
            if (mate.Entity is { } entity)
            {
                await owner.MapInstance.SendPacketAsync(entity.GenerateOut());
                await owner.MapInstance.SendPacketAsync(mate.GenerateIn(owner.AccountLanguage, entity.PositionX, entity.PositionY));
            }

            await owner.SendPacketAsync(mate.GenerateScp(owner.AccountLanguage));
            await owner.SendPacketAsync(owner.GeneratePinit());
            await owner.SendPacketsAsync(owner.Mates.Values.Where(s => s.IsTeamMember).Select(s => s.GeneratePst()));
            await owner.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.NewNameAssignedToCompanion
            });
        }
    }
}

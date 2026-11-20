//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // The client reports each bar it starts drawing, guri 2 for the player's own and guri 6 for
    // the partner's, and the map then sees whoever the bar belongs to busy until it ends.
    [UsedImplicitly]
    public sealed class DelayBarHandler
    {
        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            var character = evt.ClientSession.Character;
            if (character.PendingDelayType == null)
            {
                return Task.CompletedTask;
            }

            (VisualType Type, long Id)? busy = evt.Packet.Type switch
            {
                GuriPacketType.Dance => (VisualType.Player, character.CharacterId),
                GuriPacketType.CharacterAnimation when character.Mates.Values
                        .FirstOrDefault(s => s.MateType == MateType.Partner && s.IsTeamMember && s.Entity != null) is { } partner
                    => (VisualType.Npc, partner.MateTransportId),
                _ => null
            };
            if (busy is not { } entity)
            {
                return Task.CompletedTask;
            }

            var packet = new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Argument = (uint)entity.Type,
                EntityId = entity.Id,
                Value = 0
            };
            return character.Invisible ? character.SendPacketAsync(packet) : character.MapInstance.SendPacketAsync(packet);
        }
    }
}

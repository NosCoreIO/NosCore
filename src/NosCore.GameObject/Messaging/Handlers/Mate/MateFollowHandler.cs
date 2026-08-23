//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MateService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Messaging.Handlers.Mate
{
    // Keeps a character's mates at their heel. A mate that stays where it was summoned looks
    // broken long before it looks unfinished, so it moves on the same event the character's own
    // step publishes rather than on a timer of its own.
    [UsedImplicitly]
    public sealed class MateFollowHandler
    {
        [UsedImplicitly]
        public async Task Handle(CharacterMovedEvent evt)
        {
            // Only a player has mates, and the event is declared on the wider interface.
            if (evt.Character is not PlayerComponentBundle character)
            {
                return;
            }

            var mates = character.Mates.Values.Where(s => s.IsTeamMember).ToList();
            if (mates.Count == 0)
            {
                return;
            }

            var map = character.MapInstance;
            MatePlacement.Arrange(character.PositionX, character.PositionY, map.Map, mates);

            foreach (var mate in mates)
            {
                var move = new MovePacket
                {
                    VisualType = VisualType.Npc,
                    VisualEntityId = mate.MateTransportId,
                    MapX = mate.PositionX,
                    MapY = mate.PositionY,
                    Speed = mate.NpcMonster.Speed
                };

                // A hidden owner's mates are still theirs: broadcasting them would draw the
                // character back onto everybody's screen, and the spawn packet even names them.
                if (character.Invisible)
                {
                    await character.SendPacketAsync(move).ConfigureAwait(false);
                    continue;
                }

                await map.SendPacketAsync(move).ConfigureAwait(false);
            }
        }
    }
}

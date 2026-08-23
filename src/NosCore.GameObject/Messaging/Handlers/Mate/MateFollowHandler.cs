//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
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
        // Where a mate stands relative to its owner, tried in order. The first walkable one
        // wins, so a mate against a wall tucks in somewhere rather than refusing to move.
        private static readonly (short X, short Y)[] Offsets =
            [(1, 1), (-1, 1), (1, -1), (-1, -1), (1, 0), (-1, 0), (0, 1), (0, -1), (0, 0)];

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
            var taken = new HashSet<(short, short)>();

            foreach (var mate in mates)
            {
                var spot = Place(character, map, taken);
                mate.PositionX = spot.X;
                mate.PositionY = spot.Y;
                taken.Add(spot);

                await map.SendPacketAsync(new MovePacket
                {
                    VisualType = VisualType.Npc,
                    VisualEntityId = mate.MateTransportId,
                    MapX = mate.PositionX,
                    MapY = mate.PositionY,
                    Speed = mate.NpcMonster.Speed
                }).ConfigureAwait(false);
            }
        }

        private static (short X, short Y) Place(PlayerComponentBundle character,
            MapInstance map,
            HashSet<(short, short)> taken)
        {
            foreach (var offset in Offsets)
            {
                var x = (short)(character.PositionX + offset.X);
                var y = (short)(character.PositionY + offset.Y);
                if (!taken.Contains((x, y)) && map.Map.IsWalkable(x, y))
                {
                    return (x, y);
                }
            }

            // Nothing free anywhere around: stand on the owner. Two things in one square is
            // untidy, and better than a mate left behind on the far side of the map.
            return (character.PositionX, character.PositionY);
        }
    }
}

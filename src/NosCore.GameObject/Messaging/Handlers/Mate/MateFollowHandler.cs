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
    // Mates follow on the same event the owner's own step publishes.
    [UsedImplicitly]
    public sealed class MateFollowHandler
    {
        [UsedImplicitly]
        public async Task Handle(CharacterMovedEvent evt)
        {
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
                // The entity carries the position aggro and range checks read, not the packet.
                if (mate.Entity is { } handle)
                {
                    handle.PositionX = mate.PositionX;
                    handle.PositionY = mate.PositionY;
                }

                var move = new MovePacket
                {
                    VisualType = VisualType.Npc,
                    VisualEntityId = mate.MateTransportId,
                    MapX = mate.PositionX,
                    MapY = mate.PositionY,
                    Speed = mate.NpcMonster.Speed
                };

                // Broadcasting a hidden owner's mates would put the owner back on screen.
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

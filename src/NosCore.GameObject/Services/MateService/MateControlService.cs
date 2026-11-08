//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.Networking;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MateService
{
    public class MateControlService(IHeuristic distanceCalculator) : IMateControlService
    {
        public async Task<bool> MoveAsync(PlayerComponentBundle owner, long mateTransportId, short x, short y)
        {
            if (!TryGetOutMate(owner, mateTransportId, out var mate, out var entity)
                || !entity.IsAlive
                || entity.NoMove
                || !owner.MapInstance.Map.IsWalkable(x, y))
            {
                return false;
            }

            // The same one-step allowance a player's own walk gets, so a client cannot teleport its mate.
            var distance = (int)distanceCalculator.GetDistance((entity.PositionX, entity.PositionY), (x, y)) - 1;
            if (distance > mate.NpcMonster.Speed / 2)
            {
                return false;
            }

            mate.PositionX = x;
            mate.PositionY = y;
            entity.PositionX = x;
            entity.PositionY = y;

            await SendAsync(owner, new MovePacket
            {
                VisualType = VisualType.Npc,
                VisualEntityId = mate.MateTransportId,
                MapX = x,
                MapY = y,
                Speed = mate.NpcMonster.Speed
            });
            return true;
        }

        // Trusting the id alone would let a client drive somebody else's mate, or one left in the Miniland.
        private static bool TryGetOutMate(PlayerComponentBundle owner, long mateTransportId, out Mate mate,
            out MateComponentBundle entity)
        {
            entity = default;
            if (!owner.Mates.TryGetValue(mateTransportId, out mate!)
                || !mate.IsTeamMember
                || mate.Entity is not { } outMate)
            {
                return false;
            }

            entity = outMate;
            return true;
        }

        // Broadcasting a hidden owner's mates would put the owner back on screen.
        private static Task SendAsync(PlayerComponentBundle owner, IPacket packet) =>
            owner.Invisible ? owner.SendPacketAsync(packet) : owner.MapInstance.SendPacketAsync(packet);
    }
}

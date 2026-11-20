//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MateService
{
    public class MateControlService(IHeuristic distanceCalculator, IMateAttackService mateAttackService)
        : IMateControlService
    {
        public async Task<bool> MoveAsync(PlayerComponentBundle owner, long mateTransportId, short x, short y)
        {
            if (!TryGetOutMate(owner, mateTransportId, out var mate, out var entity))
            {
                return await MoveEscortAsync(owner, mateTransportId, x, y);
            }

            if (!entity.IsAlive
                || entity.NoMove
                || !owner.MapInstance.Map.IsWalkable(x, y))
            {
                return false;
            }

            // The same one-step allowance a player's own walk gets, so a client cannot teleport its mate.
            var distance = (int)distanceCalculator.GetDistance((entity.PositionX, entity.PositionY), (x, y));
            if (!SkillReach.IsWithinOneStep(distance, mate.NpcMonster.Speed))
            {
                return false;
            }

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

        private async Task<bool> MoveEscortAsync(PlayerComponentBundle owner, long npcId, short x, short y)
        {
            if (npcId is <= 0 or > int.MaxValue
                || owner.MapInstance.GetNpcById((int)npcId) is not { IsAlive: true } escort
                || escort.LeaderId != owner.CharacterId
                || escort.NoMove
                || !owner.MapInstance.Map.IsWalkable(x, y))
            {
                return false;
            }

            var distance = (int)distanceCalculator.GetDistance((escort.PositionX, escort.PositionY), (x, y));
            if (!SkillReach.IsWithinOneStep(distance, escort.Speed))
            {
                return false;
            }

            escort.PositionX = x;
            escort.PositionY = y;
            await owner.MapInstance.SendPacketAsync(new MovePacket
            {
                VisualType = VisualType.Npc,
                VisualEntityId = escort.VisualId,
                MapX = x,
                MapY = y,
                Speed = escort.Speed
            });
            return true;
        }

        public async Task<bool> TalkAsync(PlayerComponentBundle owner, long mateTransportId, string? message)
        {
            if (string.IsNullOrWhiteSpace(message) || !TryGetOutMate(owner, mateTransportId, out _, out var entity))
            {
                return false;
            }

            await SendAsync(owner, entity.GenerateSay(message, SayColorType.Default));
            return true;
        }

        public async Task<bool> AttackAsync(PlayerComponentBundle owner, long mateTransportId, IAliveEntity target)
        {
            if (target.MapInstance != owner.MapInstance)
            {
                return false;
            }

            if (TryGetOutMate(owner, mateTransportId, out _, out var entity))
            {
                return await mateAttackService.BasicAttackAsync(entity, target);
            }

            return mateTransportId is > 0 and <= int.MaxValue
                && owner.MapInstance.GetNpcById((int)mateTransportId) is { IsAlive: true } escort
                && escort.LeaderId == owner.CharacterId
                && await mateAttackService.BasicAttackAsync(escort, target);
        }

        // Trusting the id alone would let a client drive somebody else's mate, or one left in the Miniland.
        private static bool TryGetOutMate(PlayerComponentBundle owner, long mateTransportId, [NotNullWhen(true)] out Mate? mate,
            out MateComponentBundle entity)
        {
            entity = default;
            if (!owner.Mates.TryGetValue(mateTransportId, out mate)
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

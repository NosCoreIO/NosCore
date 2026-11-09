//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Mates
{
    public class UpetPacketHandler(
        IMateAttackService mateAttackService,
        ISessionRegistry sessionRegistry,
        ILogger<UpetPacketHandler> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<UpetPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UpetPacket packet, ClientSession session)
        {
            var character = session.Character;

            // Trusting the id would let a client drive somebody else's pet.
            if (!character.Mates.TryGetValue(packet.MateTransportId, out var mate)
                || !mate.IsTeamMember
                || mate.Entity is not { } attacker)
            {
                return;
            }

            var target = ResolveTarget(packet, session);
            if (target == null)
            {
                return;
            }

            await mateAttackService.BasicAttackAsync(attacker, target);
        }

        private IAliveEntity? ResolveTarget(UpetPacket packet, ClientSession session)
        {
            var map = session.Character.MapInstance;
            IAliveEntity? candidate = packet.TargetType switch
            {
                VisualType.Player => sessionRegistry.TryGetCharacter(s => s.VisualId == packet.TargetId, out var player)
                    ? player
                    : null,
                VisualType.Npc => map.FindNpc(s => s.VisualId == packet.TargetId),
                VisualType.Monster => map.FindMonster(s => s.VisualId == packet.TargetId),
                _ => null
            };

            if (candidate == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
            }

            return candidate;
        }
    }
}

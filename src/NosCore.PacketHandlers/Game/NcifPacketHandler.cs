//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class NcifPacketHandler(ILogger<NcifPacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry)
        : PacketHandler<NcifPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(NcifPacket ncifPacket, ClientSession session)
        {
            IAliveEntity? entity;

            switch (ncifPacket.Type)
            {
                case VisualType.Player:
                    entity = sessionRegistry.TryGetCharacter(s => s.VisualId == ncifPacket.TargetId, out var player) ? player : null;
                    break;
                case VisualType.Monster:
                    entity = session.Character.MapInstance.FindMonster(s => s.VisualId == ncifPacket.TargetId);
                    break;
                case VisualType.Npc:
                    entity = session.Character.MapInstance.FindNpc(s => s.VisualId == ncifPacket.TargetId)
                        ?? FindMate(session, ncifPacket.TargetId);
                    break;
                default:
                    logger.LogError(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        ncifPacket.Type);
                    return;
            }

            if (entity != null)
            {
                await session.SendPacketAsync(entity.GenerateStatInfo());
            }
        }

        // Mates are drawn as NPCs but live on their owner rather than in the map's NPC list.
        private IAliveEntity? FindMate(ClientSession session, long transportId)
        {
            var mapInstanceId = session.Character.MapInstanceId;
            return sessionRegistry
                .GetSessions(s => s.HasPlayerEntity && s.Character.MapInstanceId == mapInstanceId)
                .Select(s => s.Character.Mates.TryGetValue(transportId, out var mate) ? mate.Entity : null)
                .FirstOrDefault(s => s != null);
        }
    }
}

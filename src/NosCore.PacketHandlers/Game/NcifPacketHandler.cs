//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class NcifPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry)
        : PacketHandler<NcifPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(NcifPacket ncifPacket, ClientSession session)
        {
            IAliveEntity? entity;

            switch (ncifPacket.Type)
            {
                case VisualType.Player:
                    entity = sessionRegistry.GetCharacter(s => s.VisualId == ncifPacket.TargetId);
                    break;
                case VisualType.Monster:
                    entity = session.Character.MapInstance.Monsters.Find(s => s.VisualId == ncifPacket.TargetId);
                    break;
                case VisualType.Npc:
                    entity = session.Character.MapInstance.Npcs.Find(s => s.VisualId == ncifPacket.TargetId);
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        ncifPacket.Type);
                    return;
            }

            if (entity != null)
            {
                await session.SendPacketAsync(entity.GenerateStatInfo());
            }
        }
    }
}

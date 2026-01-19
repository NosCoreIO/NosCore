//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class SizePacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<SizePacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SizePacket sizePacket, ClientSession session)
        {
            IAliveEntity entity;
            switch (sizePacket.VisualType)
            {
                case VisualType.Player:
                    entity = session.Character;
                    break;
                case VisualType.Monster:
                    entity = session.Character.MapInstance.Monsters.Find(s => s.VisualId == sizePacket.VisualId)!;
                    break;
                case VisualType.Npc:
                    entity = session.Character.MapInstance.Npcs.Find(s => s.VisualId == sizePacket.VisualId)!;
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        sizePacket.VisualType);
                    return Task.CompletedTask;
            }

            if (entity == null)
            {
                logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST],
                    sizePacket.VisualType);
                return Task.CompletedTask;
            }
            entity.Size = sizePacket.Size;
            return session.Character.MapInstance.SendPacketAsync(entity.GenerateCharSc());
        }
    }
}

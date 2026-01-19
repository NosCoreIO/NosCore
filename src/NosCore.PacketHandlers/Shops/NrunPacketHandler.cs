//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.NRunService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class NrunPacketHandler(ILogger logger, INrunService nRunRunnerService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry)
        : PacketHandler<NrunPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(NrunPacket nRunPacket, ClientSession clientSession)
        {
            var forceNull = false;
            IAliveEntity? aliveEntity;
            switch (nRunPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = sessionRegistry.GetCharacter(s => s.VisualId == nRunPacket.VisualId);
                    break;
                case VisualType.Npc:
                    aliveEntity = clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == nRunPacket.VisualId);
                    break;
                case null:
                    aliveEntity = null;
                    forceNull = true;
                    break;

                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        nRunPacket.Type);
                    return;
            }

            if ((aliveEntity == null) && !forceNull)
            {
                logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return;
            }

            await nRunRunnerService.NRunLaunchAsync(clientSession, new Tuple<IAliveEntity, NrunPacket>(aliveEntity!, nRunPacket));
        }
    }
}

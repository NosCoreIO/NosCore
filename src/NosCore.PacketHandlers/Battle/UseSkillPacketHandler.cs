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
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Battle
{
    public class UseSkillPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, IBattleService battleService, ISessionRegistry sessionRegistry)
        : PacketHandler<UseSkillPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UseSkillPacket packet, ClientSession clientSession)
        {

            if (clientSession.Character.CanFight)
            {
                if (clientSession.Character.IsSitting)
                {
                    await clientSession.Character.RestAsync();
                }
                if (clientSession.Character.IsVehicled)
                {
                    await clientSession.SendPacketAsync(new CancelPacket()
                    {
                        Type = CancelPacketType.CancelAutoAttack
                    });
                    return;
                }

                IAliveEntity? requestableEntity;
                switch (packet.TargetVisualType)
                {
                    case VisualType.Player:
                        requestableEntity = sessionRegistry.GetCharacter(s => s.VisualId == packet.TargetId);
                        break;
                    case VisualType.Npc:
                        requestableEntity =
                            clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == packet.TargetId);
                        break;
                    case VisualType.Monster:
                        requestableEntity =
                            clientSession.Character.MapInstance.Monsters.Find(s => s.VisualId == packet.TargetId);
                        break;
                    default:
                        logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                            packet.TargetVisualType);
                        return;
                }

                if (requestableEntity == null)
                {
                    logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                    return;
                }

                await battleService.Hit(clientSession.Character, requestableEntity, new HitArguments()
                {
                    SkillId = packet.CastId,
                    MapX = packet.MapX,
                    MapY = packet.MapY,
                });
            }
            else
            {
                await clientSession.SendPacketAsync(new CancelPacket()
                {
                    Type = CancelPacketType.CancelAutoAttack
                });
            }
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.PacketHandlers.Game
{
    // Handles the client req_info packet (right-click on a player/npc/mate to open the
    // info card). Mirrors OpenNos's BasicPacketHandler.ReqInfo (BasicPacketHandler.cs:703):
    //
    //   case PlayerInfo (1)  -> reply with tc_info built from the targeted player
    //   case NpcInfo (5)     -> reply with e_info for the npc monster (NOT YET — needs
    //                           an EInfoPacketType.Npc value in NosCore.Packets and an
    //                           NPC-shaped e_info packet variant)
    //   case MateInfo (6)    -> reply with e_info for the mate (NOT YET — mate subsystem
    //                           is not implemented in NosCore)
    //
    // The Npc and Mate branches log a warning and no-op for now so the gap is observable
    // when a player triggers them.
    public sealed class ReqInfoPacketHandler(ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry)
        : PacketHandler<ReqInfoPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ReqInfoPacket packet, ClientSession session)
        {
            switch (packet.ReqType)
            {
                case ReqInfoType.PlayerInfo:
                    if (sessionRegistry.TryGetCharacter(s => s.VisualId == packet.TargetVNum, out var target))
                    {
                        await session.SendPacketAsync(target.GenerateReqInfo());
                    }
                    return;

                case ReqInfoType.NpcInfo:
                case ReqInfoType.MateInfo:
                    // Both reply with an e_info packet in OpenNos (see Mate.cs:157 and
                    // NpcMonster.cs:45). NosCore.Packets's EInfoPacket schema is item-
                    // oriented (no NPC/Mate variant), so honoring this requires either a
                    // packets-repo bump (add EInfoPacketType.Npc/Mate plus the NPC stat
                    // fields) or a raw-string emission. Logging until then.
                    logger.Warning(logLanguage[LogLanguageKey.UNHANDLED_REQINFO_TYPE], packet.ReqType);
                    return;

                default:
                    logger.Warning(logLanguage[LogLanguageKey.UNHANDLED_REQINFO_TYPE], packet.ReqType);
                    return;
            }
        }
    }
}

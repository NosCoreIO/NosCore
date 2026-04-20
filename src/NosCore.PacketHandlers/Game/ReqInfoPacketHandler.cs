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
    // info card). Mirrors OpenNos's BasicPacketHandler.ReqInfo:
    //
    //   case PlayerInfo (1)  -> reply with tc_info built from the targeted player
    //   case NpcInfo (5)     -> reply with e_info 10 ... for the npc monster
    //   case MateInfo (6)    -> reply with e_info 10 ... for the mate
    //
    // NpcInfo and MateInfo share the same 26-field e_info subtype-10 layout in OpenNos
    // (EInfoNpcMonsterPacket from NosCore.Packets). The mate subsystem isn't wired in
    // NosCore yet, so MateInfo is logged at Debug and no-ops until a character's runtime
    // mate list exists.
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
                    var npc = session.Character.MapInstance.FindNpc(n => n.VisualId == packet.TargetVNum);
                    if (npc is null || npc.Value.NpcMonster is null)
                    {
                        return;
                    }
                    await session.SendPacketAsync(npc.Value.NpcMonster.GenerateNpcInfo(session.Character.AccountLanguage));
                    return;

                case ReqInfoType.MateInfo:
                    // No runtime Mates collection on ICharacterEntity yet; skip quietly
                    // instead of spamming WARN until the mate subsystem lands.
                    logger.Debug("req_info MateInfo received for {Target} but mate subsystem is not wired", packet.TargetVNum);
                    return;

                default:
                    logger.Warning(logLanguage[LogLanguageKey.UNHANDLED_REQINFO_TYPE], packet.ReqType);
                    return;
            }
        }
    }
}

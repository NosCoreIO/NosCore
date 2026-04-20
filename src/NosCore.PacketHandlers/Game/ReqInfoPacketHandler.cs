//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.PacketHandlers.Game
{
    // Handles the client req_info packet (right-click on a player/npc/monster/mate to open
    // the info card). Mirrors OpenNos's BasicPacketHandler.ReqInfo but adapted for the
    // wire shape NosCore's client actually emits (observed from a live session):
    //
    //   req_info 1 <characterId>              -> tc_info for the targeted player
    //   req_info 5 <npcMonsterVNum>           -> e_info for the npc template (OpenNos stock)
    //   req_info 6 <visualType> <visualId>    -> e_info for the npc/monster on the map
    //
    // NosCore.Packets parses the type-6 wire layout into (ReqType=6, TargetVNum=visualType,
    // MateVNum=visualId) — the field names are misleading; "TargetVNum" is actually the
    // VisualType discriminator (1/2/3/…) and "MateVNum" is the VisualId. OpenNos stock
    // only supports mates under type 6, but the NosCore client reuses 6 for any entity,
    // so we dispatch on VisualType and use the same EInfoNpcMonsterPacket shape for both
    // NPCs and monsters (they share the NpcMonsterDto template).
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
                    await HandleEntityInfoAsync(packet, session);
                    return;

                default:
                    logger.Warning(logLanguage[LogLanguageKey.UNHANDLED_REQINFO_TYPE], packet.ReqType);
                    return;
            }
        }

        // Type 6: `req_info 6 <visualType> <visualId>`. TargetVNum carries the visual-type
        // discriminator; MateVNum carries the VisualId of the clicked entity. Look up the
        // template based on visual type and emit e_info.
        private async Task HandleEntityInfoAsync(ReqInfoPacket packet, ClientSession session)
        {
            if (!packet.MateVNum.HasValue)
            {
                return;
            }

            var visualType = (VisualType)(int)packet.TargetVNum;
            var visualId = packet.MateVNum.Value;
            NpcMonsterDto? template = null;
            switch (visualType)
            {
                case VisualType.Npc:
                    var npcLookup = session.Character.MapInstance.FindNpc(n => n.VisualId == visualId);
                    template = npcLookup?.NpcMonster;
                    break;
                case VisualType.Monster:
                    var monsterLookup = session.Character.MapInstance.FindMonster(m => m.VisualId == visualId);
                    template = monsterLookup?.NpcMonster;
                    break;
                default:
                    logger.Debug("req_info 6 for unsupported visualType={VisualType} visualId={VisualId}", visualType, visualId);
                    return;
            }

            if (template is null)
            {
                return;
            }

            await session.SendPacketAsync(template.GenerateNpcInfo(session.Character.AccountLanguage));
        }
    }
}

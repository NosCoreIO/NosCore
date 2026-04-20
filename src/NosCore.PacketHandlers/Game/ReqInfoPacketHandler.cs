//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
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
    // Handles the client `req_info` packet. Dispatch mirrors the official NosTale wire
    // shape captured from a live session (OpenNos routed the same way):
    //
    //   req_info 1 <characterId>                    -> tc_info for the targeted player
    //   req_info 5 <npcMonsterVNum>                 -> e_info for the NPC monster TEMPLATE
    //                                                   (static catalog lookup, not map-scoped)
    //   req_info 6 <visualType> <visualId>          -> e_info for the LIVE entity on the
    //                                                   current map. TargetVNum is the
    //                                                   VisualType discriminator (1/2/3/…)
    //                                                   and MateVNum carries the VisualId.
    //
    // Type 6 is not mate-only — it's "info for any map entity"; the VisualType field picks
    // between player/npc/monster/mate. For NPC (2) and monster (3) branches we use the same
    // NpcMonsterDto template the in-packet carries.
    public sealed class ReqInfoPacketHandler(ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry,
            List<NpcMonsterDto> npcMonsters)
        : PacketHandler<ReqInfoPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ReqInfoPacket packet, ClientSession session)
        {
            switch (packet.ReqType)
            {
                case ReqInfoType.PlayerInfo:
                    if (sessionRegistry.TryGetCharacter(s => s.VisualId == packet.TargetVNum, out var target))
                    {
                        await session.SendPacketAsync(target.GenerateReqInfo()).ConfigureAwait(false);
                    }
                    return;

                case ReqInfoType.NpcInfo:
                    // Static catalog lookup by NpcMonsterVNum — the client passes a VNum, not
                    // a map-scoped VisualId, because this request is used for tooltip/info
                    // on any NPC the client knows about (shop menus, inventory hover, etc).
                    var template = npcMonsters.FirstOrDefault(n => n.NpcMonsterVNum == (short)packet.TargetVNum);
                    if (template is null)
                    {
                        return;
                    }
                    await session.SendPacketAsync(template.GenerateNpcInfo(session.Character.AccountLanguage)).ConfigureAwait(false);
                    return;

                case ReqInfoType.MateInfo:
                    if (packet.MateVNum.HasValue)
                    {
                        await HandleMapEntityInfoAsync(packet, session).ConfigureAwait(false);
                    }
                    else
                    {
                        await HandleMateInfoAsync(packet, session).ConfigureAwait(false);
                    }
                    return;

                default:
                    logger.Warning(logLanguage[LogLanguageKey.UNHANDLED_REQINFO_TYPE], packet.ReqType);
                    return;
            }
        }

        // `req_info 6 <visualType> <visualId>` — live-map entity lookup. TargetVNum is the
        // VisualType byte (1 player, 2 npc, 3 monster, 9 pet/mate) and MateVNum is the
        // VisualId of the clicked entity on the current map.
        private async Task HandleMapEntityInfoAsync(ReqInfoPacket packet, ClientSession session)
        {
            var visualType = (VisualType)(int)packet.TargetVNum;
            var visualId = packet.MateVNum!.Value;
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

            await session.SendPacketAsync(template.GenerateNpcInfo(session.Character.AccountLanguage)).ConfigureAwait(false);
        }

        // `req_info 6 <mateTransportId>` (single-arg form) — mate / partner info card.
        // OpenNos BasicPacketHandler looks up Session.Character.Mates by MateTransportId and
        // calls mate.GenerateEInfo. NosCore has no runtime Mates collection on the character
        // yet (see Database/Entities/Mate.cs), so we log the miss and no-op — OpenNos also
        // no-ops when the lookup returns null, so this keeps byte-for-byte parity until the
        // mate subsystem lands.
        private Task HandleMateInfoAsync(ReqInfoPacket packet, ClientSession session)
        {
            logger.Debug("req_info 6 <mateTransportId={TransportId}> received but mate subsystem is not wired",
                packet.TargetVNum);
            return Task.CompletedTask;
        }
    }
}

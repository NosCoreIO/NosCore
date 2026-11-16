//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    // A teleport is captured as the n_run the player sent to the NPC followed by the at packet of the
    // map they arrived on; the option index is the n_run type.
    public class TeleporterParser(IDao<TeleporterDto, short> teleporterDao, IDao<MapNpcDto, int> mapNpcDao,
        ILogger<TeleporterParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public static readonly IReadOnlySet<NrunRunnerType> TeleportRunners = new HashSet<NrunRunnerType>
        {
            NrunRunnerType.Teleport,
            NrunRunnerType.TeleportActV,
            NrunRunnerType.GoToWeddingGazebo,
            NrunRunnerType.GoToGlacerusCave,
            NrunRunnerType.GoToForgottenArchipelago,
            NrunRunnerType.ReturnFromMortazDesert,
            NrunRunnerType.ReturnFromGlacernon,
        };

        public async Task InsertTeleportersAsync(IReadOnlyList<IPacket> packets)
        {
            var npcs = mapNpcDao.LoadAll().ToDictionary(s => s.MapNpcId);
            var stored = teleporterDao.LoadAll().ToList();
            var captured = new Dictionary<(int MapNpcId, NrunRunnerType Runner, short Index), TeleporterDto>();
            (int MapNpcId, NrunRunnerType Runner, short Index)? pending = null;

            foreach (var packet in packets)
            {
                if (packet is NrunPacket nrun)
                {
                    pending = TeleportRunners.Contains(nrun.Runner) && nrun is { Type: { } index, VisualId: { } npcId }
                        && npcs.ContainsKey((int)npcId) ? ((int)npcId, nrun.Runner, index) : null;
                    continue;
                }

                if (packet is not AtPacket at || pending == null)
                {
                    continue;
                }

                var key = pending.Value;
                pending = null;
                if (captured.ContainsKey(key))
                {
                    continue;
                }

                captured[key] = new TeleporterDto
                {
                    MapNpcId = key.MapNpcId,
                    Runner = key.Runner,
                    Index = key.Index,
                    MapId = at.MapId,
                    MapX = at.PositionX,
                    MapY = at.PositionY,
                    Type = at.MapId == npcs[key.MapNpcId].MapId ? TeleporterType.TeleporterOnMap : TeleporterType.Teleporter,
                };
            }

            // The same NPC was often recorded under several ids, and only one of them was used for a
            // given option, so its copies take the destinations it has.
            foreach (var group in npcs.Values.GroupBy(s => (s.MapId, s.VNum, s.Dialog)))
            {
                var known = captured.Where(t => group.Any(n => n.MapNpcId == t.Key.MapNpcId))
                    .GroupBy(t => (t.Key.Runner, t.Key.Index)).Select(g => g.First()).ToList();
                foreach (var npc in group)
                {
                    foreach (var (key, teleporter) in known.Where(t => !captured.ContainsKey((npc.MapNpcId, t.Key.Runner, t.Key.Index))))
                    {
                        captured[(npc.MapNpcId, key.Runner, key.Index)] = new TeleporterDto
                        {
                            MapNpcId = npc.MapNpcId,
                            Runner = teleporter.Runner,
                            Index = teleporter.Index,
                            MapId = teleporter.MapId,
                            MapX = teleporter.MapX,
                            MapY = teleporter.MapY,
                            Type = teleporter.Type,
                        };
                    }
                }
            }

            // The capture holds every destination of the NPCs it shows; a destination stored before its
            // runner was recorded takes the runner it was captured under.
            var capturedNpcs = CaptureSessions.NpcIds(packets);
            var teleporters = new List<TeleporterDto>();
            var kept = new HashSet<short>();
            foreach (var teleporter in captured.Values)
            {
                var row = stored.FirstOrDefault(s => !kept.Contains(s.TeleporterId) && s.MapNpcId == teleporter.MapNpcId && s.Runner == teleporter.Runner && s.Index == teleporter.Index)
                    ?? stored.FirstOrDefault(s => !kept.Contains(s.TeleporterId) && s.MapNpcId == teleporter.MapNpcId && s.Runner == null && s.Index == teleporter.Index);
                if (row == null)
                {
                    teleporters.Add(teleporter);
                    continue;
                }

                kept.Add(row.TeleporterId);
                if ((row.Runner, row.MapId, row.MapX, row.MapY, row.Type) != (teleporter.Runner, teleporter.MapId, teleporter.MapX, teleporter.MapY, teleporter.Type))
                {
                    row.Runner = teleporter.Runner;
                    row.MapId = teleporter.MapId;
                    row.MapX = teleporter.MapX;
                    row.MapY = teleporter.MapY;
                    row.Type = teleporter.Type;
                    teleporters.Add(row);
                }
            }

            await teleporterDao.TryDeleteAsync(stored
                .Where(s => capturedNpcs.Contains(s.MapNpcId) && !kept.Contains(s.TeleporterId))
                .Select(s => s.TeleporterId).ToList()).OrThrowAsync();
            await teleporterDao.TryInsertOrUpdateAsync(teleporters).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.TELEPORTERS_PARSED], teleporters.Count);
        }
    }
}

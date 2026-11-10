//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
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

        public async Task InsertTeleportersAsync(List<string[]> packetList)
        {
            var npcs = mapNpcDao.LoadAll().ToDictionary(s => s.MapNpcId);
            var stored = teleporterDao.LoadAll().ToList();
            var captured = new Dictionary<(int MapNpcId, NrunRunnerType Runner, short Index), TeleporterDto>();
            (int MapNpcId, NrunRunnerType Runner, short Index)? pending = null;

            foreach (var packet in packetList)
            {
                if (packet.Length > 4 && packet[0] == "n_run" && TryGetTeleportRunner(packet[1], out var runner)
                    && short.TryParse(packet[2], out var index) && int.TryParse(packet[4], out var npcId))
                {
                    pending = npcs.ContainsKey(npcId) ? (npcId, runner, index) : null;
                    continue;
                }

                if (packet.Length <= 5 || packet[0] != "at" || pending == null)
                {
                    continue;
                }

                var key = pending.Value;
                pending = null;
                if (captured.ContainsKey(key))
                {
                    continue;
                }

                var mapId = short.Parse(packet[2]);
                captured[key] = new TeleporterDto
                {
                    MapNpcId = key.MapNpcId,
                    Runner = key.Runner,
                    Index = key.Index,
                    MapId = mapId,
                    MapX = short.Parse(packet[3]),
                    MapY = short.Parse(packet[4]),
                    Type = mapId == npcs[key.MapNpcId].MapId ? TeleporterType.TeleporterOnMap : TeleporterType.Teleporter,
                };
            }

            // The same NPC was often recorded under several ids, and only one of them was used for a
            // given option, so its copies take the destinations it has.
            foreach (var group in npcs.Values.GroupBy(s => (s.MapId, s.VNum, s.Dialog)))
            {
                var known = captured.Values.Where(t => group.Any(n => n.MapNpcId == t.MapNpcId))
                    .GroupBy(t => (t.Runner, t.Index)).Select(g => g.First()).ToList();
                foreach (var npc in group)
                {
                    foreach (var teleporter in known.Where(t => !captured.ContainsKey((npc.MapNpcId, t.Runner!.Value, t.Index))))
                    {
                        captured[(npc.MapNpcId, teleporter.Runner!.Value, teleporter.Index)] = new TeleporterDto
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

            // A destination stored before its runner was recorded takes the runner it was captured under.
            var teleporters = new List<TeleporterDto>();
            foreach (var teleporter in captured.Values)
            {
                if (stored.Any(s => s.MapNpcId == teleporter.MapNpcId && s.Runner == teleporter.Runner && s.Index == teleporter.Index))
                {
                    continue;
                }

                var withoutRunner = stored.FirstOrDefault(s => s.MapNpcId == teleporter.MapNpcId && s.Runner == null && s.Index == teleporter.Index);
                if (withoutRunner != null)
                {
                    stored.Remove(withoutRunner);
                    withoutRunner.Runner = teleporter.Runner;
                    teleporters.Add(withoutRunner);
                    continue;
                }

                teleporters.Add(teleporter);
            }

            await teleporterDao.TryInsertOrUpdateAsync(teleporters);
            logger.LogInformation(logLanguage[LogLanguageKey.TELEPORTERS_PARSED], teleporters.Count);
        }

        private static bool TryGetTeleportRunner(string value, out NrunRunnerType runner)
        {
            runner = (NrunRunnerType)(short.TryParse(value, out var raw) ? raw : 0);
            return TeleportRunners.Contains(runner);
        }
    }
}

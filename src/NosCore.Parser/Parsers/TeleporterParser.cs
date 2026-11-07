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
        };

        public async Task InsertTeleportersAsync(List<string[]> packetList)
        {
            var npcs = mapNpcDao.LoadAll().ToDictionary(s => s.MapNpcId);
            var existing = teleporterDao.LoadAll().Select(s => (s.MapNpcId, s.Index)).ToHashSet();
            var captured = new Dictionary<(int MapNpcId, short Index), TeleporterDto>();
            (int MapNpcId, short Index)? pending = null;

            foreach (var packet in packetList)
            {
                if (packet.Length > 4 && packet[0] == "n_run" && IsTeleportRunner(packet[1])
                    && short.TryParse(packet[2], out var index) && int.TryParse(packet[4], out var npcId))
                {
                    pending = npcs.ContainsKey(npcId) ? (npcId, index) : null;
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
                    .GroupBy(t => t.Index).Select(g => g.First()).ToList();
                foreach (var npc in group)
                {
                    foreach (var teleporter in known.Where(t => !captured.ContainsKey((npc.MapNpcId, t.Index))))
                    {
                        captured[(npc.MapNpcId, teleporter.Index)] = new TeleporterDto
                        {
                            MapNpcId = npc.MapNpcId,
                            Index = teleporter.Index,
                            MapId = teleporter.MapId,
                            MapX = teleporter.MapX,
                            MapY = teleporter.MapY,
                            Type = teleporter.Type,
                        };
                    }
                }
            }

            var teleporters = captured.Where(s => !existing.Contains(s.Key)).Select(s => s.Value).ToList();
            await teleporterDao.TryInsertOrUpdateAsync(teleporters);
            logger.LogInformation(logLanguage[LogLanguageKey.TELEPORTERS_PARSED], teleporters.Count);
        }

        private static bool IsTeleportRunner(string runner) =>
            short.TryParse(runner, out var value) && TeleportRunners.Contains((NrunRunnerType)value);
    }
}

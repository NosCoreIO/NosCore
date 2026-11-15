//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // A stored row on a map the capture shows that the capture no longer holds is a copy or a retired
    // NPC from an earlier import, so it is removed with its shops, teleports and recipes; the NPC's own
    // row is brought in line with the capture.
    public class MapNpcParser(IDao<MapNpcDto, int> mapNpcDao, IDao<NpcMonsterDto, short> npcMonsterDao, IDao<NpcTalkDto, short> npcTalkDao,
        IDao<ShopDto, int> shopDao, IDao<ShopItemDto, int> shopItemDao, IDao<ShopSkillDto, int> shopSkillDao,
        IDao<TeleporterDto, short> teleporterDao, IDao<RecipeDto, short> recipeDao, IDao<RecipeItemDto, short> recipeItemDao,
        ILogger<MapNpcParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertMapNpcsAsync(IReadOnlyList<IPacket> packets)
        {
            var npcmonsterdb = npcMonsterDao.LoadAll().ToDictionary(n => n.NpcMonsterVNum);
            var stored = mapNpcDao.LoadAll().ToDictionary(s => s.MapNpcId);
            short map = 0;
            var npcs = new List<MapNpcDto>();
            var npcTalks = npcTalkDao.LoadAll().ToDictionary(s => s.DialogId, s => s);
            foreach (var packet in packets)
            {
                if (packet is AtPacket at)
                {
                    map = at.MapId;
                    continue;
                }

                if (packet is not InPacket { VisualType: VisualType.Npc, VisualId: <= 20000, InNonPlayerSubPacket: { } npc } inPacket
                    || !short.TryParse(inPacket.VNum, out var vnum) || !npcmonsterdb.TryGetValue(vnum, out var npcMonster))
                {
                    continue;
                }

                var npctest = new MapNpcDto
                {
                    MapX = inPacket.PositionX,
                    MapY = inPacket.PositionY,
                    MapId = map,
                    VNum = vnum,
                    MapNpcId = (int)inPacket.VisualId,
                    Effect = npcMonster.EffectIdConstantly,
                    EffectDelay = 4750,
                    IsMoving = npcMonster.CanWalk,
                    Direction = inPacket.Direction ?? 0,
                    Dialog = npcTalks.ContainsKey(npc.Dialog) ? npc.Dialog : null,
                    IsSitting = npc.IsSitting,
                    IsDisabled = false
                };

                if (npcs.Any(i => i.MapNpcId == npctest.MapNpcId))
                {
                    continue;
                }

                npcs.Add(npctest);
            }

            var ids = npcs.Select(s => s.MapNpcId).ToHashSet();
            var maps = CaptureSessions.Maps(packets);
            await DeleteCopiesAsync(stored.Values
                .Where(s => !ids.Contains(s.MapNpcId) && maps.Contains(s.MapId))
                .Select(s => s.MapNpcId).ToList());

            var changed = npcs.Where(s => !stored.TryGetValue(s.MapNpcId, out var row) || !Fields(row).Equals(Fields(s))).ToList();
            await mapNpcDao.TryInsertOrUpdateAsync(changed);
            logger.LogInformation(logLanguage[LogLanguageKey.NPCS_PARSED], changed.Count);
        }

        private static object Fields(MapNpcDto npc) => (npc.MapId, npc.VNum, npc.MapX, npc.MapY, npc.Effect, npc.EffectDelay,
            npc.IsMoving, npc.Direction, npc.Dialog, npc.IsSitting, npc.IsDisabled);

        private async Task DeleteCopiesAsync(List<int> copies)
        {
            if (copies.Count == 0)
            {
                return;
            }

            await CapturedRows.DeleteShopsAsync(shopDao, shopItemDao, shopSkillDao,
                shopDao.Where(s => copies.Contains(s.MapNpcId))?.Select(s => s.ShopId).ToList() ?? []);
            await teleporterDao.TryDeleteAsync(teleporterDao.Where(s => copies.Contains(s.MapNpcId))?.Select(s => s.TeleporterId).ToList() ?? []);
            await CapturedRows.DeleteRecipesAsync(recipeDao, recipeItemDao,
                recipeDao.Where(s => s.MapNpcId != null && copies.Contains(s.MapNpcId.Value))?.Select(s => s.RecipeId).ToList() ?? []);
            await mapNpcDao.TryDeleteAsync(copies);
        }
    }
}

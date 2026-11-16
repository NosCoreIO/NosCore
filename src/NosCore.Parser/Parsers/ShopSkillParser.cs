//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // A skill shop's n_inv lists bare skill vnums after the tab it answers, where an item
    // shop lists dotted item entries.
    public class ShopSkillParser(IDao<ShopSkillDto, int> shopSkillDao, IDao<ShopDto, int> shopDao,
        ILogger<ShopSkillParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertShopSkillsAsync(IReadOnlyList<IPacket> packets)
        {
            var shopdb = shopDao.LoadAll().ToList();
            var shops = shopdb.GroupBy(s => s.MapNpcId).ToDictionary(g => g.Key, g => g.First());
            var skills = new List<ShopSkillDto>();
            foreach (var packet in packets.OfType<NInvPacket>())
            {
                if (packet.Skills is not { } listed || !shops.TryGetValue((int)packet.VisualId, out var shop) || packet.ShopKind is not { } type)
                {
                    continue;
                }

                foreach (var skillVNum in listed)
                {
                    var tab = skills.Where(s => s.ShopId == shop.ShopId && s.Type == type).ToList();
                    if (tab.Any(s => s.SkillVNum == skillVNum))
                    {
                        continue;
                    }

                    skills.Add(new ShopSkillDto
                    {
                        ShopId = shop.ShopId,
                        Type = type,
                        Slot = (byte)tab.Count,
                        SkillVNum = skillVNum
                    });
                }
            }

            // The capture is the whole list of the shops of the NPCs it shows.
            var capturedNpcs = CaptureSessions.NpcIds(packets);
            var capturedShops = shopdb.Where(s => capturedNpcs.Contains(s.MapNpcId)).Select(s => s.ShopId).ToHashSet();
            var stored = shopSkillDao.LoadAll().ToList();
            var desired = skills.Select(s => (s.ShopId, s.Type, s.SkillVNum)).ToHashSet();
            await shopSkillDao.TryDeleteAsync(stored
                .Where(s => capturedShops.Contains(s.ShopId) && !desired.Contains((s.ShopId, s.Type, s.SkillVNum)))
                .Select(s => s.ShopSkillId).ToList()).OrThrowAsync();

            var storedByKey = stored.GroupBy(s => (s.ShopId, s.Type, s.SkillVNum)).ToDictionary(g => g.Key, g => g.First());
            var changed = new List<ShopSkillDto>();
            foreach (var skill in skills)
            {
                if (!storedByKey.TryGetValue((skill.ShopId, skill.Type, skill.SkillVNum), out var row))
                {
                    changed.Add(skill);
                }
                else if (row.Slot != skill.Slot)
                {
                    row.Slot = skill.Slot;
                    changed.Add(row);
                }
            }

            await shopSkillDao.TryInsertOrUpdateAsync(changed).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.SHOPSKILLS_PARSED], changed.Count);
        }
    }
}

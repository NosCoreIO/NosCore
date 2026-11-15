//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
            var shops = shopDao.LoadAll().GroupBy(s => s.MapNpcId).ToDictionary(g => g.Key, g => g.First());
            var skills = shopSkillDao.LoadAll().ToList();
            var added = new List<ShopSkillDto>();
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

                    var shopSkill = new ShopSkillDto
                    {
                        ShopId = shop.ShopId,
                        Type = type,
                        Slot = (byte)tab.Count,
                        SkillVNum = skillVNum
                    };
                    skills.Add(shopSkill);
                    added.Add(shopSkill);
                }
            }

            await shopSkillDao.TryInsertOrUpdateAsync(added);
            logger.LogInformation(logLanguage[LogLanguageKey.SHOPSKILLS_PARSED], added.Count);
        }
    }
}

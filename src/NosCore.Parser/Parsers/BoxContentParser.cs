//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    public class BoxContentParser(IDao<ItemDto, short> itemDao,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao, ILogger<BoxContentParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private const short LowestRare = -2;
        private const short HighestRare = 8;

        public async Task InsertBoxContentsAsync()
        {
            var items = itemDao.LoadAll().ToDictionary(i => i.VNum);
            var rolls = new List<RollGeneratedItemDto>();
            var boxes = new List<ItemDto>();
            foreach (var item in items.Values.OrderBy(i => i.VNum))
            {
                var box = BoxContents.All.GetValueOrDefault(item.VNum) ?? RaidBoxOf(item);
                if (box == null || box.Sets.Any(set => set.Any(c => !items.ContainsKey(c.VNum))))
                {
                    continue;
                }

                item.Effect = box.Effect;
                boxes.Add(item);
                rolls.AddRange(box.Sets.SelectMany((set, group) => set.Select(c => new RollGeneratedItemDto
                {
                    OriginalItemVNum = item.VNum,
                    OriginalItemDesign = box.Design,
                    MinimumOriginalItemRare = LowestRare,
                    MaximumOriginalItemRare = HighestRare,
                    ItemGeneratedVNum = c.VNum,
                    ItemGeneratedAmount = c.Amount,
                    IsRareRandom = box.IsRaidBox && items[c.VNum].ItemType is ItemType.Weapon or ItemType.Armor,
                    Probability = 1,
                    RollGroup = (short)group,
                })));
            }

            for (var i = 0; i < rolls.Count; i++)
            {
                rolls[i].RollGeneratedItemId = (short)(i + 1);
            }

            await rollGeneratedItemDao.TryDeleteAsync(rollGeneratedItemDao.LoadAll().Select(r => r.RollGeneratedItemId).ToList());
            await itemDao.TryInsertOrUpdateAsync(boxes);
            await rollGeneratedItemDao.TryInsertOrUpdateAsync(rolls);
            logger.LogInformation(logLanguage[LogLanguageKey.BOX_CONTENTS_PARSED], boxes.Count, rolls.Count);
        }

        // Raid number 0 is also what boxes outside any raid carry, so it never names a raid here.
        private static BoxContent? RaidBoxOf(ItemDto item) =>
            item.EffectValue > 0 && (item.Effect == ItemEffectType.SimpleRaidBox || item is { Effect: ItemEffectType.BoxEffect, ItemType: ItemType.Box })
                ? BoxContents.Raids.GetValueOrDefault(item.EffectValue)
                : null;
    }
}

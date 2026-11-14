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
            foreach (var (vnum, (effect, contents)) in BoxContents.All)
            {
                if (!items.TryGetValue(vnum, out var box) || contents.Any(c => !items.ContainsKey(c.VNum)))
                {
                    continue;
                }

                box.Effect = effect;
                boxes.Add(box);
                rolls.AddRange(contents.Select(c => new RollGeneratedItemDto
                {
                    OriginalItemVNum = vnum,
                    ItemGeneratedVNum = c.VNum,
                    ItemGeneratedAmount = c.Amount,
                    Probability = 1,
                }));
            }

            foreach (var (vnum, contents) in RaidBoxes.All)
            {
                if (!items.TryGetValue(vnum, out var box) || contents.Any(c => !items.ContainsKey(c.VNum)))
                {
                    continue;
                }

                box.Effect = ItemEffectType.BoxEffect;
                boxes.Add(box);
                rolls.AddRange(contents.Select(c => new RollGeneratedItemDto
                {
                    OriginalItemVNum = vnum,
                    OriginalItemDesign = RaidBoxes.Design,
                    MinimumOriginalItemRare = LowestRare,
                    MaximumOriginalItemRare = HighestRare,
                    ItemGeneratedVNum = c.VNum,
                    ItemGeneratedAmount = c.Amount,
                    IsRareRandom = items[c.VNum].ItemType is ItemType.Weapon or ItemType.Armor,
                    Probability = 1,
                }));
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
    }
}

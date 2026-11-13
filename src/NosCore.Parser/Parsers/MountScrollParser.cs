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

namespace NosCore.Parser.Parsers
{
    public class MountScrollParser(IDao<ItemDto, short> itemDao,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao, ILogger<MountScrollParser> logger)
    {
        public async Task InsertMountScrollsAsync()
        {
            var items = itemDao.LoadAll().ToDictionary(i => i.VNum);
            var nextId = (short)((rollGeneratedItemDao.LoadAll().Select(r => (int)r.RollGeneratedItemId).DefaultIfEmpty().Max()) + 1);
            var scrolls = new List<ItemDto>();
            var rolls = new List<RollGeneratedItemDto>();
            foreach (var (vnum, scroll) in MountScrolls.All)
            {
                if (!items.TryGetValue(vnum, out var item) || scroll.Mounts.Any(m => !items.ContainsKey(m)))
                {
                    continue;
                }

                item.Speed = scroll.Speed;
                item.WaitDelay = 3000;
                item.ConsumedOnMount = scroll.ConsumedOnMount;
                item.LevelMaximum = scroll.LevelMaximum;
                scrolls.Add(item);
                rolls.AddRange(scroll.Mounts.Select(m => new RollGeneratedItemDto
                {
                    RollGeneratedItemId = nextId++,
                    OriginalItemVNum = vnum,
                    ItemGeneratedVNum = m,
                    ItemGeneratedAmount = 1,
                    Probability = 1,
                }));
            }

            await itemDao.TryInsertOrUpdateAsync(scrolls);
            await rollGeneratedItemDao.TryInsertOrUpdateAsync(rolls);
            logger.LogInformation("{Scrolls} mount scrolls draw from {Rolls} mounts", scrolls.Count, rolls.Count);
        }
    }
}

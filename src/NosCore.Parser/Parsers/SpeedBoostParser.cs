//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.StaticEntities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class SpeedBoostParser(IDao<ItemDto, short> itemDao, ILogger<SpeedBoostParser> logger)
    {
        private const short RandomTeleportEffect = 3625;

        public async Task InsertSpeedBoostsAsync()
        {
            var boosted = new List<ItemDto>();
            foreach (var item in itemDao.LoadAll())
            {
                if (!MountSpeedBoosts.All.TryGetValue(item.VNum, out var boost))
                {
                    continue;
                }

                item.SpeedBoost = boost.Speed;
                item.SpeedBoostDuration = boost.Duration;
                item.SpeedBoostRestoresPerLevel = boost.RestoresPerLevel;
                item.SpeedBoostRemovesDebuffsUpToLevel = boost.RemovesDebuffsUpToLevel;
                item.SpeedBoostTeleportsRandomly = boost.TeleportsRandomly;
                item.SpeedBoostDashCells = boost.DashCells;
                item.SpeedBoostTeleportEffect = boost.TeleportsRandomly ? RandomTeleportEffect : (short)0;
                boosted.Add(item);
            }

            await itemDao.TryInsertOrUpdateAsync(boosted);
            logger.LogInformation("{Mounts} mounts get their Speed Booster effect", boosted.Count);
        }
    }
}

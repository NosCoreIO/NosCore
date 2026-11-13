//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class InstantCombatParser(IDao<InstantCombatMonsterDto, short> monsterDao,
        IDao<InstantCombatDropDto, short> dropDao, ILogger<InstantCombatParser> logger)
    {
        public async Task InsertInstantCombatWavesAsync()
        {
            var monsters = InstantCombatWaves.Monsters.Select((m, i) => new InstantCombatMonsterDto
            {
                InstantCombatMonsterId = (short)(i + 1),
                LevelMinimum = m.LevelMinimum,
                Wave = m.Wave,
                NpcMonsterVNum = m.NpcMonsterVNum,
                Amount = m.Amount,
                IsMoving = m.IsMoving,
            }).ToList();
            var drops = InstantCombatWaves.Drops.Select((d, i) => new InstantCombatDropDto
            {
                InstantCombatDropId = (short)(i + 1),
                LevelMinimum = d.LevelMinimum,
                Wave = d.Wave,
                ItemVNum = d.ItemVNum,
                Piles = d.Piles,
                Amount = d.Amount,
            }).ToList();

            await monsterDao.TryDeleteAsync(monsterDao.LoadAll().Select(m => m.InstantCombatMonsterId).ToList());
            await dropDao.TryDeleteAsync(dropDao.LoadAll().Select(d => d.InstantCombatDropId).ToList());
            await monsterDao.TryInsertOrUpdateAsync(monsters);
            await dropDao.TryInsertOrUpdateAsync(drops);
            logger.LogInformation("{Monsters} Instant Combat monster lines and {Drops} drop lines imported", monsters.Count, drops.Count);
        }
    }
}

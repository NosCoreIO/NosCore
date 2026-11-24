//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//



using Microsoft.Extensions.Logging;
using NosCore.Core.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class UpgradeCostParser(IDao<ItemDto, short> itemDao, IDao<UpgradeCostDto, short> upgradeCostDao,
        IDao<UpgradeCostMaterialDto, short> upgradeCostMaterialDao, ILogger<UpgradeCostParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertUpgradeCostsAsync()
        {
            var items = itemDao.LoadAll().Select(s => s.VNum).ToHashSet();
            var costs = new List<UpgradeCostDto>();
            var materials = new List<UpgradeCostMaterialDto>();
            short costId = 0;
            short materialId = 0;
            foreach (var cost in UpgradeCosts.All)
            {
                costId++;
                if (cost.Materials.Any(m => !items.Contains(m.ItemVNum)))
                {
                    materialId += (short)cost.Materials.Length;
                    continue;
                }

                costs.Add(new UpgradeCostDto
                {
                    UpgradeCostId = costId,
                    Kind = cost.Kind,
                    Gold = cost.Gold,
                });
                materials.AddRange(cost.Materials.Select(m => new UpgradeCostMaterialDto
                {
                    UpgradeCostMaterialId = ++materialId,
                    UpgradeCostId = costId,
                    ItemVNum = m.ItemVNum,
                    Amount = m.Amount,
                }));
            }

            var materialIds = materials.Select(s => s.UpgradeCostMaterialId).ToHashSet();
            await upgradeCostMaterialDao.TryDeleteAsync(upgradeCostMaterialDao.LoadAll()
                .Where(s => !materialIds.Contains(s.UpgradeCostMaterialId)).Select(s => s.UpgradeCostMaterialId).ToList()).OrThrowAsync();
            var costIds = costs.Select(s => s.UpgradeCostId).ToHashSet();
            await upgradeCostDao.TryDeleteAsync(upgradeCostDao.LoadAll()
                .Where(s => !costIds.Contains(s.UpgradeCostId)).Select(s => s.UpgradeCostId).ToList()).OrThrowAsync();
            await upgradeCostDao.TryInsertOrUpdateAsync(costs).OrThrowAsync();
            await upgradeCostMaterialDao.TryInsertOrUpdateAsync(materials).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.UPGRADE_COSTS_PARSED], costs.Count);
        }
    }
}

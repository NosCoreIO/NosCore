//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;

namespace NosCore.Parser.Parsers
{
    // Rows that hang off a captured NPC, removed with the shop or recipe they belong to.
    public static class CapturedRows
    {
        public static async Task DeleteShopsAsync(IDao<ShopDto, int> shopDao, IDao<ShopItemDto, int> shopItemDao,
            IDao<ShopSkillDto, int> shopSkillDao, IReadOnlyCollection<int> shopIds)
        {
            if (shopIds.Count == 0)
            {
                return;
            }

            await shopItemDao.TryDeleteAsync(shopItemDao.Where(s => shopIds.Contains(s.ShopId))?.Select(s => s.ShopItemId).ToList() ?? []).OrThrowAsync();
            await shopSkillDao.TryDeleteAsync(shopSkillDao.Where(s => shopIds.Contains(s.ShopId))?.Select(s => s.ShopSkillId).ToList() ?? []).OrThrowAsync();
            await shopDao.TryDeleteAsync(shopIds).OrThrowAsync();
        }

        public static async Task DeleteRecipesAsync(IDao<RecipeDto, short> recipeDao, IDao<RecipeItemDto, short> recipeItemDao,
            IReadOnlyCollection<short> recipeIds)
        {
            if (recipeIds.Count == 0)
            {
                return;
            }

            await recipeItemDao.TryDeleteAsync(recipeItemDao.Where(s => recipeIds.Contains(s.RecipeId))?.Select(s => s.RecipeItemId).ToList() ?? []).OrThrowAsync();
            await recipeDao.TryDeleteAsync(recipeIds).OrThrowAsync();
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.BoxService
{
    public sealed class BoxRollService(IItemCatalog itemCatalog, IDao<RollGeneratedItemDto, short> rollGeneratedItemDao,
        IRandomProvider random) : IBoxRollService
    {
        public IReadOnlyList<RollGeneratedItemDto>? Roll(IItemInstance box, PlayerComponentBundle opener)
        {
            var sets = (rollGeneratedItemDao.Where(r =>
                    r.OriginalItemVNum == box.ItemVNum
                    && r.MinimumOriginalItemRare <= box.Rare
                    && r.MaximumOriginalItemRare >= box.Rare
                    && r.OriginalItemDesign == box.Design
                    && r.Probability > 0)
                ?? Enumerable.Empty<RollGeneratedItemDto>())
                .GroupBy(r => r.RollGroup)
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(r => r.ItemGeneratedVNum).ToList())
                .ToList();

            if (box.Item.Effect == ItemEffectType.ClassBox)
            {
                var fitting = sets.Where(set => set.All(r => itemCatalog.GetItem(r.ItemGeneratedVNum) is { } item
                    && item.FitsClass(opener.Class) && item.FitsGender(opener.Gender))).ToList();
                if (fitting.Count > 0)
                {
                    sets = fitting;
                }
            }

            var sum = sets.Sum(set => (int)set[0].Probability);
            if (sum <= 0)
            {
                return null;
            }

            var roll = random.Next(0, sum);
            var cumulative = 0;
            foreach (var set in sets)
            {
                cumulative += set[0].Probability;
                if (roll < cumulative)
                {
                    return set;
                }
            }

            return null;
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
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
    // n_run 14 opens an NPC's production window and m_list 2 lists what it makes; pdtse 1
    // asks for one of those and m_list 3 answers with the amount made and the materials.
    public class RecipeParser(IDao<RecipeDto, short> recipeDao, IDao<RecipeItemDto, short> recipeItemDao,
        IDao<MapNpcDto, int> mapNpcDao, IDao<ItemDto, short> itemDao, ILogger<RecipeParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private const string OpenProductionRunner = "14";
        private const string RecipeList = "2";
        private const string RecipeMaterials = "3";
        private const string ShowRecipe = "1";

        public async Task InsertRecipesAsync(List<string[]> packetList)
        {
            var npcIds = mapNpcDao.LoadAll().Select(s => s.MapNpcId).ToHashSet();
            var itemVNums = itemDao.LoadAll().Select(s => s.VNum).ToHashSet();
            var existing = recipeDao.LoadAll().Select(s => (s.MapNpcId, s.ItemVNum)).ToHashSet();

            var listed = new HashSet<(int MapNpcId, short ItemVNum)>();
            var recipes = new Dictionary<(int MapNpcId, short ItemVNum), (byte Amount, List<RecipeItemDto> Items)>();
            int? mapNpcId = null;
            short? shownItem = null;

            foreach (var packet in packetList.Where(s => s.Length > 2))
            {
                switch (packet[0])
                {
                    case "n_run" when packet.Length > 4:
                        mapNpcId = packet[1] == OpenProductionRunner && int.TryParse(packet[4], out var npcId) ? npcId : null;
                        break;

                    case "m_list" when packet[1] == RecipeList && mapNpcId != null:
                        foreach (var vnum in packet.Skip(2))
                        {
                            if (short.TryParse(vnum, out var itemVNum) && itemVNum > 0)
                            {
                                listed.Add((mapNpcId.Value, itemVNum));
                            }
                        }
                        break;

                    case "pdtse":
                        shownItem = packet[1] == ShowRecipe && short.TryParse(packet[2], out var shown) ? shown : null;
                        break;

                    case "m_list" when packet[1] == RecipeMaterials:
                        if (mapNpcId != null && shownItem != null && listed.Contains((mapNpcId.Value, shownItem.Value))
                            && !recipes.ContainsKey((mapNpcId.Value, shownItem.Value)) && byte.TryParse(packet[2], out var amount))
                        {
                            var items = new List<RecipeItemDto>();
                            for (var i = 3; i + 1 < packet.Length; i += 2)
                            {
                                items.Add(new RecipeItemDto
                                {
                                    ItemVNum = short.Parse(packet[i]),
                                    Amount = short.Parse(packet[i + 1])
                                });
                            }

                            recipes[(mapNpcId.Value, shownItem.Value)] = (amount, items);
                        }

                        shownItem = null;
                        break;
                }
            }

            var count = 0;
            var recipeItems = new List<RecipeItemDto>();
            foreach (var ((npc, itemVNum), (amount, items)) in recipes)
            {
                if (existing.Contains((npc, itemVNum)) || !npcIds.Contains(npc) || !itemVNums.Contains(itemVNum)
                    || items.Count == 0 || items.Any(s => !itemVNums.Contains(s.ItemVNum)))
                {
                    continue;
                }

                var recipe = await recipeDao.TryInsertOrUpdateAsync(new RecipeDto
                {
                    MapNpcId = npc,
                    ItemVNum = itemVNum,
                    Amount = amount
                });
                if (recipe == null)
                {
                    continue;
                }

                items.ForEach(s => s.RecipeId = recipe.RecipeId);
                recipeItems.AddRange(items);
                count++;
            }

            await recipeItemDao.TryInsertOrUpdateAsync(recipeItems);
            logger.LogInformation(logLanguage[LogLanguageKey.RECIPES_PARSED], count);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // n_run 14 opens an NPC's production window, and u_i on a production scroll opens the
    // scroll's; m_list 2 lists what the window makes, pdtse 1 asks for one of those and m_list 3
    // answers with the amount made and the materials.
    public class RecipeParser(IDao<RecipeDto, short> recipeDao, IDao<RecipeItemDto, short> recipeItemDao,
        IDao<MapNpcDto, int> mapNpcDao, IDao<ItemDto, short> itemDao, ILogger<RecipeParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private const string OpenProductionRunner = "14";
        private const string RecipeList = "2";
        private const string RecipeMaterials = "3";
        private const string ShowRecipe = "1";
        private const string MainPocket = "1";

        public async Task InsertRecipesAsync(List<string[]> packetList)
        {
            var npcIds = mapNpcDao.LoadAll().Select(s => s.MapNpcId).ToHashSet();
            var items = itemDao.LoadAll().ToDictionary(s => s.VNum);
            var scrolls = items.Values
                .Where(s => s.ItemType == ItemType.Production && s.Effect == ItemEffectType.OpenProduction)
                .Select(s => s.VNum).ToHashSet();
            var existing = recipeDao.LoadAll().Select(s => (s.MapNpcId, s.ScrollVNum, s.ItemVNum)).ToHashSet();

            var listed = new HashSet<(int? MapNpcId, short? ScrollVNum, short ItemVNum)>();
            var recipes = new Dictionary<(int? MapNpcId, short? ScrollVNum, short ItemVNum), (byte Amount, List<RecipeItemDto> Items)>();
            var mainPocket = new Dictionary<string, short>();
            int? mapNpcId = null;
            short? scrollVNum = null;
            short? shownItem = null;

            foreach (var packet in packetList.Where(s => s.Length > 2))
            {
                switch (packet[0])
                {
                    case "inv" when packet[1] == MainPocket:
                        mainPocket.Clear();
                        foreach (var slot in packet.Skip(2).Select(s => s.Split('.')).Where(s => s.Length > 1))
                        {
                            if (short.TryParse(slot[1], out var vnum))
                            {
                                mainPocket[slot[0]] = vnum;
                            }
                        }
                        break;

                    case "ivn" when packet[1] == MainPocket:
                        var change = packet[2].Split('.');
                        if (change.Length > 1 && short.TryParse(change[1], out var changed))
                        {
                            mainPocket[change[0]] = changed;
                        }
                        break;

                    case "n_run" when packet.Length > 4:
                        mapNpcId = packet[1] == OpenProductionRunner && int.TryParse(packet[4], out var npcId) ? npcId : null;
                        scrollVNum = null;
                        break;

                    case "u_i" when packet.Length > 4:
                        mapNpcId = null;
                        scrollVNum = packet[3] == MainPocket && mainPocket.TryGetValue(packet[4], out var used) && scrolls.Contains(used)
                            ? used : null;
                        break;

                    case "m_list" when packet[1] == RecipeList && (mapNpcId != null || scrollVNum != null):
                        foreach (var vnum in packet.Skip(2))
                        {
                            if (short.TryParse(vnum, out var itemVNum) && itemVNum > 0)
                            {
                                listed.Add((mapNpcId, scrollVNum, itemVNum));
                            }
                        }
                        break;

                    case "pdtse":
                        shownItem = packet[1] == ShowRecipe && short.TryParse(packet[2], out var shown) ? shown : null;
                        break;

                    case "m_list" when packet[1] == RecipeMaterials:
                        var key = (mapNpcId, scrollVNum, shownItem ?? 0);
                        if (shownItem != null && listed.Contains(key) && !recipes.ContainsKey(key) && byte.TryParse(packet[2], out var amount))
                        {
                            var materials = new List<RecipeItemDto>();
                            for (var i = 3; i + 1 < packet.Length; i += 2)
                            {
                                materials.Add(new RecipeItemDto
                                {
                                    ItemVNum = short.Parse(packet[i]),
                                    Amount = short.Parse(packet[i + 1])
                                });
                            }

                            recipes[key] = (amount, materials);
                        }

                        shownItem = null;
                        break;
                }
            }

            var count = 0;
            var recipeItems = new List<RecipeItemDto>();
            foreach (var ((npc, scroll, itemVNum), (amount, materials)) in recipes)
            {
                if (existing.Contains((npc, scroll, itemVNum)) || (npc != null && !npcIds.Contains(npc.Value))
                    || !items.ContainsKey(itemVNum) || materials.Count == 0 || materials.Any(s => !items.ContainsKey(s.ItemVNum)))
                {
                    continue;
                }

                var recipe = await recipeDao.TryInsertOrUpdateAsync(new RecipeDto
                {
                    MapNpcId = npc,
                    ScrollVNum = scroll,
                    ItemVNum = itemVNum,
                    Amount = amount
                });
                if (recipe == null)
                {
                    continue;
                }

                materials.ForEach(s => s.RecipeId = recipe.RecipeId);
                recipeItems.AddRange(materials);
                count++;
            }

            await recipeItemDao.TryInsertOrUpdateAsync(recipeItems);
            logger.LogInformation(logLanguage[LogLanguageKey.RECIPES_PARSED], count);
        }
    }
}

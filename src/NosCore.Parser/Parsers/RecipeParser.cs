//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Production;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // An NPC's production window opens through its runner and a scroll's when the scroll is used. The
    // window lists what it makes; showing one recipe answers with the amount made and the materials.
    public class RecipeParser(IDao<RecipeDto, short> recipeDao, IDao<RecipeItemDto, short> recipeItemDao,
        IDao<MapNpcDto, int> mapNpcDao, IDao<ItemDto, short> itemDao, ILogger<RecipeParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertRecipesAsync(IReadOnlyList<IPacket> packets)
        {
            var npcIds = mapNpcDao.LoadAll().Select(s => s.MapNpcId).ToHashSet();
            var items = itemDao.LoadAll().ToDictionary(s => s.VNum);
            var scrolls = items.Values
                .Where(s => s.ItemType == ItemType.Production && s.Effect == ItemEffectType.OpenProduction)
                .Select(s => s.VNum).ToHashSet();

            var listed = new HashSet<(int? MapNpcId, short? ScrollVNum, short ItemVNum)>();
            var recipes = new Dictionary<(int? MapNpcId, short? ScrollVNum, short ItemVNum), (byte Amount, List<RecipeItemDto> Items)>();
            var mainPocket = new Dictionary<short, short>();
            int? mapNpcId = null;
            short? scrollVNum = null;
            short? shownItem = null;

            foreach (var packet in packets)
            {
                switch (packet)
                {
                    case InvPacket { Type: PocketType.Main } inv:
                        mainPocket.Clear();
                        foreach (var slot in inv.IvnSubPackets ?? [])
                        {
                            mainPocket[slot.Slot] = slot.VNum;
                        }
                        break;

                    case IvnPacket { Type: PocketType.Main, IvnSubPackets: [var change, ..] }:
                        mainPocket[change.Slot] = change.VNum;
                        break;

                    case NrunPacket { VisualId: { } npcId } nrun:
                        mapNpcId = nrun.Runner == NrunRunnerType.OpenProduction ? (int)npcId : null;
                        scrollVNum = null;
                        break;

                    case UseItemPacket use:
                        mapNpcId = null;
                        scrollVNum = use.Type == PocketType.Main && mainPocket.TryGetValue(use.Slot, out var used) && scrolls.Contains(used)
                            ? used : null;
                        break;

                    case MlistPacket { Type: MlistType.Recipes } list when mapNpcId != null || scrollVNum != null:
                        foreach (var itemVNum in list.ItemVNums.Where(s => s > 0))
                        {
                            listed.Add((mapNpcId, scrollVNum, itemVNum));
                        }
                        break;

                    case PdtsePacket pdtse:
                        shownItem = pdtse.Type == PdtsePacketType.ShowRecipe ? pdtse.VNum : null;
                        break;

                    case MlistMaterialsPacket { Type: MlistType.Materials } recipe:
                        var key = (mapNpcId, scrollVNum, shownItem ?? 0);
                        if (shownItem != null && listed.Contains(key) && !recipes.ContainsKey(key) && recipe.Amount is >= 0 and <= byte.MaxValue)
                        {
                            recipes[key] = ((byte)recipe.Amount, recipe.Materials
                                .Select(s => new RecipeItemDto { ItemVNum = s.VNum, Amount = s.Amount }).ToList());
                        }

                        shownItem = null;
                        break;
                }
            }

            var desired = recipes
                .Where(s => (s.Key.MapNpcId == null || npcIds.Contains(s.Key.MapNpcId.Value)) && items.ContainsKey(s.Key.ItemVNum)
                    && s.Value.Items.Count > 0 && s.Value.Items.All(m => items.ContainsKey(m.ItemVNum)))
                .ToDictionary(s => s.Key, s => s.Value);

            // The capture holds every recipe of the NPCs it shows.
            var capturedNpcs = CaptureSessions.NpcIds(packets);
            var stored = recipeDao.LoadAll().ToList();
            await CapturedRows.DeleteRecipesAsync(recipeDao, recipeItemDao, stored
                .Where(s => s.MapNpcId != null && capturedNpcs.Contains(s.MapNpcId.Value) && !desired.ContainsKey((s.MapNpcId, s.ScrollVNum, s.ItemVNum)))
                .Select(s => s.RecipeId).ToList());
            var storedByKey = stored.GroupBy(s => (s.MapNpcId, s.ScrollVNum, s.ItemVNum)).ToDictionary(g => g.Key, g => g.First());
            var storedItems = (recipeItemDao.LoadAll() ?? []).ToLookup(s => s.RecipeId);

            var count = 0;
            var recipeItems = new List<RecipeItemDto>();
            foreach (var (key, (amount, materials)) in desired)
            {
                RecipeDto? recipe;
                if (storedByKey.TryGetValue(key, out var row))
                {
                    var current = storedItems[row.RecipeId].ToList();
                    if (row.Amount == amount && Materials(current).SequenceEqual(Materials(materials)))
                    {
                        continue;
                    }

                    await recipeItemDao.TryDeleteAsync(current.Select(s => s.RecipeItemId).ToList()).OrThrowAsync();
                    row.Amount = amount;
                    recipe = await recipeDao.TryInsertOrUpdateAsync(row);
                }
                else
                {
                    recipe = await recipeDao.TryInsertOrUpdateAsync(new RecipeDto
                    {
                        MapNpcId = key.MapNpcId,
                        ScrollVNum = key.ScrollVNum,
                        ItemVNum = key.ItemVNum,
                        Amount = amount
                    });
                }

                if (recipe == null)
                {
                    continue;
                }

                materials.ForEach(s => s.RecipeId = recipe.RecipeId);
                recipeItems.AddRange(materials);
                count++;
            }

            await recipeItemDao.TryInsertOrUpdateAsync(recipeItems).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.RECIPES_PARSED], count);
        }

        private static IEnumerable<(short ItemVNum, short Amount)> Materials(IEnumerable<RecipeItemDto> items) =>
            items.Select(s => (s.ItemVNum, s.Amount)).Order();
    }
}

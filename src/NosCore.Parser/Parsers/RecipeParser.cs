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
            var existing = recipeDao.LoadAll().Select(s => (s.MapNpcId, s.ScrollVNum, s.ItemVNum)).ToHashSet();

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

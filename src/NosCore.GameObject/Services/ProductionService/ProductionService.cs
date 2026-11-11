//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Production;
using NosCore.Packets.ServerPackets.UI;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ProductionService
{
    public class ProductionService(IItemGenerationService itemProvider, IHeuristic distanceCalculator,
            List<RecipeDto> recipes, List<RecipeItemDto> recipeItems, Wolverine.IMessageBus messageBus)
        : IProductionService, ISingletonService
    {
        private const int MaxDistance = 5;
        private const short ProductionSlot = 29;
        private const uint ProducedEffect = 1324;

        private readonly Dictionary<short, List<Recipe>> _scrollRecipes = LoadScrollRecipes(recipes, recipeItems);

        public Task OpenAsync(ClientSession session, NpcComponentBundle npc)
        {
            return OpenAsync(session, npc.Recipes ?? new List<Recipe>(), WindowType.Production);
        }

        public Task OpenScrollAsync(ClientSession session, short scrollVNum)
        {
            return OpenAsync(session, _scrollRecipes.GetValueOrDefault(scrollVNum) ?? new List<Recipe>(), WindowType.Production2);
        }

        private static Task OpenAsync(ClientSession session, IEnumerable<Recipe> recipes, WindowType window)
        {
            return session.SendPacketsAsync(new IPacket[]
            {
                new MlistPacket
                {
                    ItemVNums = recipes
                        .Where(s => s.Amount > 0)
                        .Select(s => s.ItemVNum)
                        .ToList()
                },
                new WopenPacket { Type = window }
            });
        }

        public Task ShowRecipeAsync(ClientSession session, short itemVNum)
        {
            var recipe = FindRecipe(session, itemVNum);
            if (recipe == null)
            {
                return Task.CompletedTask;
            }

            return session.SendPacketAsync(new MlistMaterialsPacket
            {
                Amount = recipe.Amount,
                Materials = recipe.Items.Where(s => s.Amount > 0)
                    .Select(s => new MlistMaterialSubPacket { VNum = s.ItemVNum, Amount = s.Amount })
                    .ToList()
            });
        }

        public async Task ProduceAsync(ClientSession session, short itemVNum)
        {
            var character = session.Character;
            var recipe = FindRecipe(session, itemVNum);
            if (recipe == null || !await HasMaterialsAsync(session, recipe))
            {
                return;
            }

            var produced = character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemProvider.Create(recipe.ItemVNum, recipe.Amount), character.CharacterId));
            if (produced == null || produced.Count == 0)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
                return;
            }

            var packets = new List<IPacket>();
            foreach (var material in recipe.Items)
            {
                var remaining = (int)material.Amount;
                foreach (var stack in character.InventoryService.Values
                    .Where(s => s.ItemInstance.ItemVNum == material.ItemVNum)
                    .OrderBy(s => s.Slot)
                    .ToList())
                {
                    if (remaining <= 0)
                    {
                        break;
                    }

                    var taken = (short)Math.Min(remaining, stack.ItemInstance.Amount);
                    var left = character.InventoryService.RemoveItemAmountFromInventory(taken, stack.ItemInstanceId);
                    remaining -= taken;
                    packets.Add(left.GeneratePocketChange(stack.Type.ToPocketType(), stack.Slot));
                }
            }

            packets.AddRange(produced.Select(s => s.GeneratePocketChange(s.Type.ToPocketType(), s.Slot)));
            var item = produced[0].ItemInstance;
            packets.Add(new PdtiPacket
            {
                Type = PdtiPacketType.Produced,
                Vnum = item.ItemVNum,
                RecipeAmount = recipe.Amount,
                Slot = ProductionSlot,
                ItemUpgrade = item.Upgrade,
                Rare = 0
            });
            packets.Add(new GuriPacket
            {
                Type = GuriPacketType.AfterSumming,
                Argument = 1,
                EntityId = character.CharacterId,
                Value = ProducedEffect
            });
            await session.SendPacketsAsync(packets);
            await messageBus.PublishAsync(new ItemProducedEvent(character, item.ItemVNum, recipe.Amount));
        }

        private static async Task<bool> HasMaterialsAsync(ClientSession session, Recipe recipe)
        {
            if (recipe.Items.All(s => session.Character.InventoryService.CountItem(s.ItemVNum) >= s.Amount))
            {
                return true;
            }

            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.RawMaterialsMissing
            });
            return false;
        }

        private Recipe? FindRecipe(ClientSession session, short itemVNum)
        {
            var character = session.Character;
            Recipe? recipe = null;
            character.MapInstance.FindNpc(npc =>
            {
                if (distanceCalculator.GetDistance((character.PositionX, character.PositionY), (npc.PositionX, npc.PositionY)) > MaxDistance)
                {
                    return false;
                }

                recipe = npc.Recipes?.FirstOrDefault(s => s.ItemVNum == itemVNum && s.Amount > 0);
                return recipe != null;
            });

            return recipe ?? _scrollRecipes
                .Where(s => character.InventoryService.CountItem(s.Key) > 0)
                .SelectMany(s => s.Value)
                .FirstOrDefault(s => s.ItemVNum == itemVNum && s.Amount > 0);
        }

        private static Dictionary<short, List<Recipe>> LoadScrollRecipes(List<RecipeDto> recipes, List<RecipeItemDto> recipeItems)
        {
            var items = recipeItems.GroupBy(s => s.RecipeId).ToDictionary(s => s.Key, s => s.ToList());
            return recipes.Where(s => s.ScrollVNum != null)
                .GroupBy(s => s.ScrollVNum.GetValueOrDefault())
                .ToDictionary(s => s.Key, s => s.Select(r => new Recipe
                {
                    RecipeId = r.RecipeId,
                    ScrollVNum = r.ScrollVNum,
                    ItemVNum = r.ItemVNum,
                    Amount = r.Amount,
                    Items = items.GetValueOrDefault(r.RecipeId) ?? new List<RecipeItemDto>()
                }).ToList());
        }
    }
}

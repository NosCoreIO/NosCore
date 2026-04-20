//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Ecs.Extensions
{
    public static class NonPlayableEntityExtension
    {
        public static void InitializeShopAndDialog(this NpcComponentBundle bundle,
            IReadOnlyList<ShopDto> shopDtos, NpcTalkDto? npcTalkDto, List<ShopItemDto> shopItemsDto,
            IItemGenerationService itemProvider)
        {
            var dialogId = bundle.Dialog ?? 0;
            if (bundle.Requests.TryGetValue(typeof(NpcDialogRequestSubject), out var subject))
            {
                var visualId = bundle.VisualId;
                Task RequestExecAsync(RequestData request)
                {
                    if (dialogId != 0)
                    {
                        return ((INonPlayableEntity)bundle).ShowDialogAsync(request, dialogId);
                    }
                    if (bundle.Shop != null)
                    {
                        return request.ClientSession.HandlePacketsAsync(new[]
                        {
                            new ShoppingPacket
                            {
                                VisualType = VisualType.Npc,
                                VisualId = visualId,
                                ShopType = 0,
                                Unknown = 0
                            }
                        });
                    }
                    return Task.CompletedTask;
                }
                subject.Select(RequestExecAsync).Subscribe();
            }

            if (shopDtos.Count == 0)
            {
                return;
            }

            var shops = new Dictionary<int, Shop>();
            foreach (var shopDto in shopDtos)
            {
                var shopItemsList = new ConcurrentDictionary<int, ShopItem>();
                foreach (var shopItemDto in shopItemsDto.Where(si => si.ShopId == shopDto.ShopId))
                {
                    var shopItem = shopItemDto.Adapt<ShopItem>();
                    shopItem.ItemInstance = itemProvider.Create(shopItemDto.ItemVNum, -1);
                    shopItemsList[shopItemDto.ShopItemId] = shopItem;
                }
                var shop = shopDto.Adapt<Shop>();
                shop.Name = npcTalkDto?.Name ?? bundle.NpcMonster?.Name ?? new I18NString();
                shop.OwnerCharacter = null;
                shop.ShopItems = shopItemsList;
                shops[shop.ShopId] = shop;
            }
            bundle.Shops = shops;
            bundle.Shop = shops.Values.First();
        }

        public static void StopLife(this INonPlayableEntity entity)
        {
            entity.Life?.Dispose();
            entity.Life = null;
        }

        public static Task StartLifeAsync(this INonPlayableEntity entity, IHeuristic distanceCalculator, IClock clock, ILogger logger)
            => entity.StartLifeAsync(monsterAi: null, distanceCalculator, clock, logger);

        // Overload that lets the caller wire in the aggro-driven AI. When the AI is
        // absent (or the monster has no aggro target this tick) we fall back to the
        // original random-wander behaviour. Monsters that are passive patrol as before;
        // aggroed ones pursue + attack until they leash out.
        public static Task StartLifeAsync(this INonPlayableEntity entity, IMonsterAi? monsterAi, IHeuristic distanceCalculator, IClock clock, ILogger logger)
        {
            entity.Life?.Dispose();

            async Task LifeAsync()
            {
                try
                {
                    if (entity.MapInstance.IsSleeping) return;

                    var acted = false;
                    if (monsterAi != null && entity is MonsterComponentBundle monster)
                    {
                        acted = await monsterAi.TickAsync(monster);
                    }

                    if (!acted)
                    {
                        await entity.MoveAsync(distanceCalculator, clock);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e.Message, e);
                }
            }
            entity.Life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Select(_ => LifeAsync()).Subscribe();
            return Task.CompletedTask;
        }

        public static Task ShowDialogAsync(this INonPlayableEntity entity, RequestData requestData, long dialog)
        {
            return requestData.ClientSession.SendPacketAsync(AliveEntityExtension.GenerateNpcReq(entity, dialog));
        }
    }
}

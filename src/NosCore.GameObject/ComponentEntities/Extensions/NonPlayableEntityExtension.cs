//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.ShopService;
using NosCore.PathFinder.Interfaces;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class NonPlayableEntityExtension
    {
        public static void Initialize(this INonPlayableEntity entity, NpcMonsterDto npcMonster)
        {
            entity.NpcMonster = npcMonster;
            entity.Mp = npcMonster?.MaxMp ?? 0;
            entity.Hp = npcMonster?.MaxHp ?? 0;
            entity.PositionX = entity.MapX;
            entity.PositionY = entity.MapY;
            entity.IsAlive = true;
            entity.Level = npcMonster?.Level ?? 0;
        }

        public static void Initialize(this INonPlayableEntity entity, NpcMonsterDto npcMonster,
            ShopDto? shopDto, NpcTalkDto? npcTalkDto, List<ShopItemDto> shopItemsDto,
            IItemGenerationService itemProvider)
        {
            entity.NpcMonster = npcMonster;
            entity.Mp = npcMonster?.MaxMp ?? 0;
            entity.Hp = npcMonster?.MaxHp ?? 0;
            entity.Speed = npcMonster?.Speed ?? 0;
            entity.PositionX = entity.MapX;
            entity.PositionY = entity.MapY;
            entity.IsAlive = true;

            if (entity is IRequestableEntity requestableEntity && entity is MapNpc mapNpc)
            {
                Task RequestExecAsync(RequestData request)
                {
                    return entity.ShowDialogAsync(request, mapNpc.Dialog ?? 0);
                }
                requestableEntity.Requests[typeof(INrunEventHandler)]?.Select(RequestExecAsync).Subscribe();
            }

            if (shopDto == null)
            {
                return;
            }

            var shopItemsList = new ConcurrentDictionary<int, ShopItem>();
            Parallel.ForEach(shopItemsDto, shopItemGrouping =>
            {
                var shopItem = shopItemGrouping.Adapt<ShopItem>();
                shopItem.ItemInstance = itemProvider.Create(shopItemGrouping.ItemVNum, -1);
                shopItemsList[shopItemGrouping.ShopItemId] = shopItem;
            });
            entity.Shop = shopDto.Adapt<Shop>();
            entity.Shop.Name = npcTalkDto?.Name ?? new I18NString();
            entity.Shop.OwnerCharacter = null;
            entity.Shop.ShopItems = shopItemsList;
        }

        public static void StopLife(this INonPlayableEntity entity)
        {
            entity.Life?.Dispose();
            entity.Life = null;
        }

        public static Task StartLifeAsync(this INonPlayableEntity entity, IHeuristic distanceCalculator, IClock clock, ILogger logger)
        {
            entity.Life?.Dispose();

            async Task LifeAsync()
            {
                try
                {
                    if (!entity.MapInstance.IsSleeping)
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

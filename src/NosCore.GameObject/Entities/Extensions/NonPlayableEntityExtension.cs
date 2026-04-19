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
using NosCore.GameObject.Entities.Interfaces;
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

namespace NosCore.GameObject.Entities.Extensions
{
    public static class NonPlayableEntityExtension
    {
        public static void InitializeShopAndDialog(this NpcComponentBundle bundle,
            ShopDto? shopDto, NpcTalkDto? npcTalkDto, List<ShopItemDto> shopItemsDto,
            IItemGenerationService itemProvider)
        {
            var dialogId = bundle.Dialog ?? 0;
            if (bundle.Requests.TryGetValue(typeof(INrunEventHandler), out var subject))
            {
                Task RequestExecAsync(RequestData request)
                {
                    return ((INonPlayableEntity)bundle).ShowDialogAsync(request, dialogId);
                }
                subject.Select(RequestExecAsync).Subscribe();
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
            var shop = shopDto.Adapt<Shop>();
            shop.Name = npcTalkDto?.Name ?? new I18NString();
            shop.OwnerCharacter = null;
            shop.ShopItems = shopItemsList;
            bundle.Shop = shop;
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

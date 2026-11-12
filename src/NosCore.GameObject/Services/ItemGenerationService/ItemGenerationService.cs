//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Mapster;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ItemGenerationService
{
    public class ItemGenerationService(
        List<ItemDto> items,
        ILoggerFactory loggerFactory,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IRarityPointService rarityPoints) : IItemGenerationService
    {
        public IItemInstance Convert(IItemInstanceDto k)
        {
            var itemDto = items.Find(s => s.VNum == k.ItemVNum)
                ?? throw new InvalidOperationException(logLanguage[LogLanguageKey.UNBOUND_ITEM_DETECTED]);
            var item = itemDto.Adapt<Item.Item>();
            IItemInstance instance = k switch
            {
                BoxInstanceDto _ => new BoxInstance(item),
                SpecialistInstanceDto _ => new SpecialistInstance(item),
                WearableInstanceDto _ => new WearableInstance(item, loggerFactory.CreateLogger<WearableInstance>(), logLanguage),
                UsableInstanceDto _ => new UsableInstance(item),
                _ => new ItemInstance(item)
            };
            k.Adapt(instance, k.GetType(), instance.GetType());
            return instance;
        }

        public IItemInstance Create(short itemToCreateVNum) => Create(itemToCreateVNum, 1);

        public IItemInstance Create(short itemToCreateVNum, short amount) => Create(itemToCreateVNum, amount, 0);

        public IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare) => Create(itemToCreateVNum, amount, rare, 0);

        public IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare, byte upgrade)
            => Create(itemToCreateVNum, amount, rare, upgrade, 0);

        public IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare, byte upgrade, byte design)
            => Generate(itemToCreateVNum, amount, rare, upgrade, design);

        public IItemInstance Generate(short itemToCreateVNum, short amount, sbyte rare, byte upgrade, byte design)
        {
            var itemToCreate = (items.Find(s => s.VNum == itemToCreateVNum)
                ?? throw new InvalidOperationException(logLanguage[LogLanguageKey.UNBOUND_ITEM_DETECTED])).Adapt<Item.Item>();
            switch (itemToCreate.Type)
            {
                case NoscorePocketType.Miniland:
                    return new ItemInstance(itemToCreate)
                    {
                        Amount = amount,
                        DurabilityPoint = itemToCreate.MinilandObjectPoint / 2
                    };

                case NoscorePocketType.Equipment:
                    switch (itemToCreate.ItemType)
                    {
                        case ItemType.Specialist:
                            return new SpecialistInstance(itemToCreate)
                            {
                                SpLevel = 1,
                                Amount = amount,
                                Design = design,
                                Upgrade = upgrade
                            };
                        case ItemType.Box:
                            return new BoxInstance(itemToCreate)
                            {
                                Amount = amount,
                                Upgrade = upgrade,
                                Design = design
                            };
                        default:
                            var wear = new WearableInstance(itemToCreate, loggerFactory.CreateLogger<WearableInstance>(), logLanguage)
                            {
                                Amount = amount,
                                Rare = rare,
                                Upgrade = upgrade,
                                Design = design
                            };
                            if (wear.Rare > 0)
                            {
                                wear.SetRarityPoint(rarityPoints);
                            }
                            return wear;
                    }

                default:
                    return new ItemInstance(itemToCreate)
                    {
                        Amount = amount
                    };
            }
        }
    }
}

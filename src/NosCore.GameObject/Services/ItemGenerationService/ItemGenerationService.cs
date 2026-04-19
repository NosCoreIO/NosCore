//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ItemGenerationService
{
    public class ItemGenerationService(
        List<ItemDto> items,
        ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IItemGenerationService
    {
        public IItemInstance Convert(IItemInstanceDto k)
        {
            IItemInstance item = k switch
            {
                BoxInstanceDto _ => k.Adapt<BoxInstance>(),
                SpecialistInstanceDto _ => k.Adapt<SpecialistInstance>(),
                WearableInstanceDto _ => k.Adapt<WearableInstance>(),
                UsableInstanceDto _ => k.Adapt<UsableInstance>(),
                _ => k.Adapt<ItemInstance>()
            };
            var itemDto = items.Find(s => s.VNum == k.ItemVNum);
            if (itemDto == null)
            {
                throw new InvalidOperationException(logLanguage[LogLanguageKey.UNBOUND_ITEM_DETECTED]);
            }
            item.Item = itemDto.Adapt<Item.Item>();
            return item;
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
            var itemToCreate = items.Find(s => s.VNum == itemToCreateVNum)!.Adapt<Item.Item>();
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
                            var wear = new WearableInstance(itemToCreate, logger, logLanguage)
                            {
                                Amount = amount,
                                Rare = rare,
                                Upgrade = upgrade,
                                Design = design
                            };
                            if (wear.Rare > 0)
                            {
                                wear.SetRarityPoint();
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

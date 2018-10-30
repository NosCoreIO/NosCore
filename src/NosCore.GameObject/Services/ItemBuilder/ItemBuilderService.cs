//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using Mapster;
using NosCore.Data;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.Services.ItemBuilder
{
    public class ItemBuilderService : IItemBuilderService
    {
        private readonly List<Item.Item> _items;

        public ItemBuilderService(List<Item.Item> items)
        {
            _items = items;
        }

        public IItemInstance Convert(IItemInstanceDto k)
        {
            IItemInstance item =
                k.Adapt<BoxInstance>() ??
                k.Adapt<SpecialistInstance>() ??
                k.Adapt<WearableInstance>() ??
                k.Adapt<UsableInstance>() ??
                (IItemInstance)k.Adapt<ItemInstance>();

            item.Item = _items.Find(s => s.VNum == k.ItemVNum);
            return item;
        }

        public IItemInstance Create(short itemToCreateVNum, long characterId, short amount = 1, sbyte rare = 0,
            byte upgrade = 0, byte design = 0)
        {
            Item.Item itemToCreate = _items.Find(s => s.VNum == itemToCreateVNum);
            switch (itemToCreate.Type)
            {
                case PocketType.Miniland:
                    return new ItemInstance(itemToCreate)
                    {
                        CharacterId = characterId,
                        Amount = amount,
                        DurabilityPoint = itemToCreate.MinilandObjectPoint / 2
                    };

                case PocketType.Equipment:
                    switch (itemToCreate.ItemType)
                    {
                        case ItemType.Specialist:
                            return new SpecialistInstance(itemToCreate)
                            {
                                SpLevel = 1,
                                Amount = amount,
                                CharacterId = characterId,
                                Design = design,
                                Upgrade = upgrade
                            };
                        case ItemType.Box:
                            return new BoxInstance(itemToCreate)
                            {
                                Amount = amount,
                                CharacterId = characterId,
                                Upgrade = upgrade,
                                Design = design
                            };
                        default:
                            var wear = new WearableInstance(itemToCreate)
                            {
                                Amount = amount,
                                Rare = rare,
                                CharacterId = characterId,
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
                        Type = itemToCreate.Type,
                        Amount = amount,
                        CharacterId = characterId
                    };
            }
        }
    }
}
//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using Mapster;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.ItemGenerationService
{
    public sealed class ItemCatalog : IItemCatalog, ISingletonService
    {
        private readonly FrozenDictionary<short, Item.Item> _items;
        private readonly IReadOnlyDictionary<ItemEffectType, IReadOnlyList<ItemDto>> _byEffect;
        private readonly IReadOnlyDictionary<MaterialType, short> _materials;

        public ItemCatalog(List<ItemDto> items)
        {
            _items = items.GroupBy(i => i.VNum).ToFrozenDictionary(g => g.Key, g => ToItem(g.First()));
            _byEffect = _items.Values
                .Where(i => i.Effect != ItemEffectType.NoEffect)
                .GroupBy(i => i.Effect)
                .ToFrozenDictionary(g => g.Key, g => (IReadOnlyList<ItemDto>)g.OrderBy(i => i.VNum).ToArray());
            _materials = _items.Values
                .Where(i => i.Material != MaterialType.None)
                .GroupBy(i => i.Material)
                .ToDictionary(g => g.Key, g => g.Min(i => i.VNum));
        }

        // Some hosts map without names, so the definition carries its names over itself.
        private static Item.Item ToItem(ItemDto dto)
        {
            var item = dto.Adapt<Item.Item>();
            item.Name = dto.Name;
            return item;
        }

        public Item.Item? GetItem(short vnum) => _items.GetValueOrDefault(vnum);

        public ItemDto? GetItemWithEffect(ItemEffectType effect) => GetItemsWithEffect(effect).FirstOrDefault();

        public IReadOnlyList<ItemDto> GetItemsWithEffect(ItemEffectType effect) =>
            _byEffect.TryGetValue(effect, out var items) ? items : [];

        public short GetMaterialVNum(MaterialType material) => _materials.GetValueOrDefault(material);
    }
}

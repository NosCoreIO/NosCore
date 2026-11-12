//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.ItemGenerationService
{
    public sealed class ItemCatalog : IItemCatalog, ISingletonService
    {
        private readonly IReadOnlyDictionary<short, ItemDto> _items;
        private readonly IReadOnlyDictionary<ItemEffectType, IReadOnlyList<ItemDto>> _byEffect;
        private readonly IReadOnlyDictionary<MaterialType, short> _materials;

        public ItemCatalog(List<ItemDto> items)
        {
            _items = items.GroupBy(i => i.VNum).ToDictionary(g => g.Key, g => g.First());
            _byEffect = _items.Values
                .Where(i => i.Effect != ItemEffectType.NoEffect)
                .GroupBy(i => i.Effect)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ItemDto>)g.OrderBy(i => i.VNum).ToArray());
            _materials = _items.Values
                .Where(i => i.Material != MaterialType.None)
                .GroupBy(i => i.Material)
                .ToDictionary(g => g.Key, g => g.Min(i => i.VNum));
        }

        public ItemDto? GetItem(short vnum) => _items.GetValueOrDefault(vnum);

        public ItemDto? GetItemWithEffect(ItemEffectType effect) => GetItemsWithEffect(effect).FirstOrDefault();

        public IReadOnlyList<ItemDto> GetItemsWithEffect(ItemEffectType effect) =>
            _byEffect.TryGetValue(effect, out var items) ? items : [];

        public short GetMaterialVNum(MaterialType material) => _materials.GetValueOrDefault(material);
    }
}

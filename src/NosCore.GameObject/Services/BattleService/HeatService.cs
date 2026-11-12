//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

public sealed class HeatService(IBuffService buffService, ICardCatalog cardCatalog, List<MapTypeDto> mapTypes,
        List<MapTypeMapDto> mapTypeMaps)
    : IHeatService, ISingletonService
{
    private readonly Dictionary<short, short> _zoneCardByMap = mapTypeMaps
        .Join(mapTypes.Where(t => t.ZoneCardId.HasValue), m => m.MapTypeId, t => t.MapTypeId,
            (m, t) => (m.MapId, CardId: t.ZoneCardId.GetValueOrDefault()))
        .GroupBy(m => m.MapId)
        .ToDictionary(g => g.Key, g => g.First().CardId);

    private readonly HashSet<short> _zoneCards = mapTypes
        .Where(t => t.ZoneCardId.HasValue).Select(t => t.ZoneCardId.GetValueOrDefault()).ToHashSet();

    public async Task RefreshAsync(PlayerComponentBundle character)
    {
        var buffs = buffService.GetActiveBuffs(character);
        var resisted = buffs.Any(b => cardCatalog.IsGrantedBy(b.CardId, ItemEffectType.HeatResistance));
        short? zoneCard = !resisted && _zoneCardByMap.TryGetValue(character.MapInstance.Map.MapId, out var cardId)
            ? cardId
            : null;

        foreach (var stale in buffs.Where(b => _zoneCards.Contains(b.CardId) && b.CardId != zoneCard)
                     .Select(b => b.CardId).ToList())
        {
            await buffService.RemoveAsync(character, stale);
        }

        if (zoneCard is { } wanted && buffs.All(b => b.CardId != wanted) && cardCatalog.GetCard(wanted) is { } card)
        {
            await buffService.ApplyPermanentAsync(character, card, cardCatalog.GetCardBCards(wanted));
        }
    }
}

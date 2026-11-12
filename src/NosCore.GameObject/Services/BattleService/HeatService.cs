//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

public sealed class HeatService(IBuffService buffService, ICardCatalog cardCatalog, List<MapTypeMapDto> mapTypeMaps)
    : IHeatService, ISingletonService
{
    private readonly HashSet<short> _hotMaps = mapTypeMaps
        .Where(m => m.MapTypeId == (short)MapTypeType.Act52)
        .Select(m => m.MapId)
        .ToHashSet();

    public async Task RefreshAsync(ICharacterEntity character)
    {
        var hot = _hotMaps.Contains(character.MapInstance.Map.MapId);
        var burning = buffService.HasBuff(character, HeatResistance.BurningCardId);
        if (hot && !burning && !buffService.HasBuff(character, HeatResistance.CardId)
            && cardCatalog.GetCard(HeatResistance.BurningCardId) is { } card)
        {
            await buffService.ApplyPermanentAsync(character, card, cardCatalog.GetCardBCards(HeatResistance.BurningCardId));
        }
        else if (!hot && burning)
        {
            await buffService.RemoveAsync(character, HeatResistance.BurningCardId);
        }
    }
}

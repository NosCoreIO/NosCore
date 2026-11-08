//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.PotionService
{
    // The delay between two potions is set per map type.
    public sealed class PotionCooldownService(
        List<MapTypeMapDto> mapTypeMaps,
        List<MapTypeDto> mapTypes,
        IClock clock) : IPotionCooldownService, ISingletonService
    {
        // What every map type outside Act 4 uses, for a map that belongs to none.
        private const short DefaultPotionDelayMs = 300;

        public bool IsReady(PlayerComponentBundle character)
        {
            var readyAt = character.LastPotion.Plus(Duration.FromMilliseconds(DelayMs(character)));
            return clock.GetCurrentInstant() >= readyAt;
        }

        public void Start(PlayerComponentBundle character)
        {
            character.LastPotion = clock.GetCurrentInstant();
        }

        private short DelayMs(PlayerComponentBundle character)
        {
            var mapId = character.MapInstance?.Map.MapId;
            if (mapId == null)
            {
                return DefaultPotionDelayMs;
            }

            var mapTypeIds = mapTypeMaps.Where(m => m.MapId == mapId).Select(m => m.MapTypeId).ToHashSet();
            var delays = mapTypes.Where(t => mapTypeIds.Contains(t.MapTypeId)).Select(t => t.PotionDelay).ToList();
            return delays.Count == 0 ? DefaultPotionDelayMs : delays.Max();
        }
    }
}

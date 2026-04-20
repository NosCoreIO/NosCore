//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.Shared.Helpers;

namespace NosCore.GameObject.Services.BattleService;

public sealed class RespawnService(
    List<MapTypeMapDto> mapTypeMaps,
    List<MapTypeDto> mapTypes,
    List<RespawnMapTypeDto> respawnMapTypes) : IRespawnService, ISingletonService
{
    private const int SpawnJitter = 3;

    public long? ResolveRespawnMapTypeId(short mapId)
    {
        var mapTypeIds = mapTypeMaps.Where(m => m.MapId == mapId).Select(m => m.MapTypeId).ToList();
        if (mapTypeIds.Count == 0) return null;
        return mapTypes.Where(t => mapTypeIds.Contains(t.MapTypeId))
            .Select(t => t.RespawnMapTypeId)
            .FirstOrDefault(id => id.HasValue);
    }

    public (short MapId, short X, short Y) GetRespawnLocation(ICharacterEntity character, long? respawnMapTypeId)
    {
        respawnMapTypeId ??= (long)RespawnType.DefaultAct1;

        var saved = character.Respawns.FirstOrDefault(r => r.RespawnMapTypeId == respawnMapTypeId);
        short mapId, baseX, baseY;
        if (saved != null)
        {
            mapId = saved.MapId;
            baseX = saved.X;
            baseY = saved.Y;
        }
        else
        {
            var catalog = respawnMapTypes.FirstOrDefault(r => r.RespawnMapTypeId == respawnMapTypeId)
                          ?? respawnMapTypes.FirstOrDefault(r => r.RespawnMapTypeId == (long)RespawnType.DefaultAct1);
            mapId = catalog?.MapId ?? 1;
            baseX = catalog?.DefaultX ?? 80;
            baseY = catalog?.DefaultY ?? 116;
        }

        var x = (short)(baseX + RandomHelper.Instance.RandomNumber(-SpawnJitter, SpawnJitter + 1));
        var y = (short)(baseY + RandomHelper.Instance.RandomNumber(-SpawnJitter, SpawnJitter + 1));
        return (mapId, x, y);
    }

    public void SetRespawnPoint(ICharacterEntity character, short mapId, short x, short y)
    {
        var respawnMapTypeId = ResolveRespawnMapTypeId(mapId);
        if (!respawnMapTypeId.HasValue) return;

        var existing = character.Respawns.FirstOrDefault(r => r.RespawnMapTypeId == respawnMapTypeId.Value);
        if (existing == null)
        {
            character.Respawns.Add(new RespawnDto
            {
                RespawnId = character.Respawns.Count == 0 ? 1 : character.Respawns.Max(r => r.RespawnId) + 1,
                CharacterId = character.CharacterId,
                MapId = mapId,
                X = x,
                Y = y,
                RespawnMapTypeId = respawnMapTypeId.Value,
            });
        }
        else
        {
            existing.MapId = mapId;
            existing.X = x;
            existing.Y = y;
        }
    }
}

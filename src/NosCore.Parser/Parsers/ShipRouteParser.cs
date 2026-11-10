using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;

namespace NosCore.Parser.Parsers;

public class ShipRouteParser(IDao<ShipRouteDto, short> routeDao, IDao<MapDto, short> mapDao,
    IDao<MapNpcDto, int> npcDao, IDao<NpcMonsterDto, short> monsters,
    IDao<NpcTalkDto, short> dialogs)
{
    /// <summary>Validate every route and its world references before saving any route from the JSON file.</summary>
    public async Task ImportAsync(string path)
    {
        var options = new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        options.Converters.Add(new JsonStringEnumConverter());
        var routes = JsonSerializer.Deserialize<List<ShipRouteDto>>(await File.ReadAllTextAsync(path), options)
            ?? throw new InvalidDataException("Ship routes must be an array.");
        var maps = mapDao.LoadAll().ToDictionary(m => m.MapId);
        var npcs = npcDao.LoadAll().ToList();
        var captainVNums = monsters.LoadAll().Select(n => n.NpcMonsterVNum).ToHashSet();
        var dialogIds = dialogs.LoadAll().Select(d => d.DialogId).ToHashSet();
        var ids = new HashSet<short>();
        var menus = new HashSet<(NrunRunnerType, short, short, short, FactionType?)>();
        foreach (var route in routes)
        {
            if (route.ShipRouteId <= 0 || !ids.Add(route.ShipRouteId)
                || !menus.Add((route.Runner, route.SourceMapId, route.BoardingNpcVNum, route.BoardingDialog, route.RequiredFaction))
                || route.DepartureIntervalSeconds <= 0 || route.Fare < 0
                || (route.Runner != NrunRunnerType.GoToShipGlacernon && route.Runner != NrunRunnerType.GoToShipMortazDesert)
                || route.CancelRunner != (route.Runner == NrunRunnerType.GoToShipGlacernon
                    ? NrunRunnerType.CancelShipTravelGlacernon : NrunRunnerType.CancelShipTravelMortazDesert)
                || route.RequiredFaction is not (null or FactionType.Angel or FactionType.Demon)
                || !npcs.Any(n => n.MapId == route.SourceMapId && n.VNum == route.BoardingNpcVNum && n.Dialog == route.BoardingDialog)
                || !captainVNums.Contains(route.CaptainVNum) || !dialogIds.Contains(route.CaptainDialog)
                || !Walkable(maps, route.SourceMapId, route.ReturnX, route.ReturnY)
                || !Walkable(maps, route.ShipMapId, route.ShipX, route.ShipY)
                || !Walkable(maps, route.ShipMapId, route.CaptainX, route.CaptainY)
                || !Walkable(maps, route.DestinationMapId, route.DestinationX, route.DestinationY))
            {
                throw new InvalidDataException($"Invalid or duplicate ship route {route.ShipRouteId}.");
            }
        }
        var existing = routeDao.LoadAll().Where(r => !ids.Contains(r.ShipRouteId));
        if (existing.Any(r => menus.Contains((r.Runner, r.SourceMapId, r.BoardingNpcVNum, r.BoardingDialog, r.RequiredFaction))))
        {
            throw new InvalidDataException("A ship route already uses this boarding menu and faction.");
        }
        var combined = routeDao.LoadAll().Where(r => !ids.Contains(r.ShipRouteId)).Concat(routes);
        if (combined.GroupBy(r => (r.Runner, r.SourceMapId, r.BoardingNpcVNum, r.BoardingDialog))
            .Any(g => g.Count() > 1 && g.Any(r => r.RequiredFaction == null)))
        {
            throw new InvalidDataException("An unrestricted ship route overlaps a faction boarding menu.");
        }
        if (!await routeDao.TryInsertOrUpdateAsync(routes))
        {
            throw new IOException("Could not save ship routes.");
        }
    }

    /// <summary>Check map dimensions and cell flags without reading beyond the stored map grid.</summary>
    private static bool Walkable(Dictionary<short, MapDto> maps, short id, short x, short y)
    {
        if (!maps.TryGetValue(id, out var map) || map.Data.Length < 4)
        {
            return false;
        }
        var width = BitConverter.ToInt16(map.Data, 0);
        var height = BitConverter.ToInt16(map.Data, 2);
        if (x < 0 || y < 0 || x >= width || y >= height || map.Data.Length < 4 + width * height)
        {
            return false;
        }
        var cell = map.Data[4 + y * width + x];
        return cell is 0 or 2 or >= 16 and <= 19;
    }
}

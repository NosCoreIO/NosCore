using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ShipService;

public class ShipService(List<ShipRouteDto> routes, List<NpcMonsterDto> npcMonsters,
    IMapInstanceAccessorService maps, IMapInstanceRegistry registry, IMapInstanceGeneratorService generator,
    IMapChangeService mapChange, IItemGenerationService items, ISessionRegistry sessions,
    IClock clock, ILogger<ShipService> logger) : IShipService, ISingletonService
{
    public async Task InitializeAsync()
    {
        foreach (var route in routes)
        {
            var template = maps.GetBaseMapById(route.ShipMapId);
            var destination = maps.GetBaseMapById(route.DestinationMapId);
            var source = maps.GetBaseMapById(route.SourceMapId);
            if (route.DepartureIntervalSeconds <= 0 || route.Fare < 0
                || template == null || destination == null || source == null
                || !template.Map.IsWalkable(route.ShipX, route.ShipY)
                || !template.Map.IsWalkable(route.CaptainX, route.CaptainY)
                || !destination.Map.IsWalkable(route.DestinationX, route.DestinationY)
                || !source.Map.IsWalkable(route.ReturnX, route.ReturnY)
                || npcMonsters.All(n => n.NpcMonsterVNum != route.CaptainVNum))
            {
                throw new InvalidOperationException($"Invalid ship route {route.ShipRouteId}");
            }

            var ship = generator.CreateMapInstance(template.Map, Guid.NewGuid(), false, MapInstanceType.NormalInstance);
            ship.ShipRoute = route;
            ship.LoadNpcs([new MapNpcDto
            {
                MapNpcId = route.ShipRouteId,
                MapId = route.ShipMapId,
                VNum = route.CaptainVNum,
                Dialog = route.CaptainDialog,
                MapX = route.CaptainX,
                MapY = route.CaptainY,
            }], npcMonsters);
            foreach (var npc in ship.Npcs)
            {
                npc.InitializeShopAndDialog([], null, [], items);
            }
            await generator.AddMapInstanceAsync(ship);
            await ship.StartLifeAsync();
        }
    }

    public static Instant NextDeparture(Instant now, int intervalSeconds)
    {
        if (intervalSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
        }
        var seconds = now.ToUnixTimeSeconds();
        return Instant.FromUnixTimeSeconds(checked((seconds / intervalSeconds + 1) * intervalSeconds));
    }

    public async Task BoardAsync(ClientSession session, NpcComponentBundle npc, NrunRunnerType runner, short option)
    {
        var player = session.Character;
        if (option != 0 || player.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance
            || player.Hp <= 0 || player.InExchangeOrShop || player.IsDisconnecting)
        {
            return;
        }
        var offered = routes.Where(r => r.Runner == runner && r.SourceMapId == player.MapInstance.Map.MapId
            && r.BoardingNpcVNum == npc.VNum && r.BoardingDialog == npc.Dialog).ToList();
        var route = offered.FirstOrDefault(r => r.RequiredFaction == null
            || (byte)r.RequiredFaction == player.CharacterDto.Faction);
        if (route == null)
        {
            if (offered.Count != 0)
            {
                await SayAsync(session, Game18NConstString.FactionRequired);
            }
            return;
        }
        var ship = registry.GetAll().FirstOrDefault(m => m.ShipRoute?.ShipRouteId == route.ShipRouteId);
        if (ship == null || ship.Executor.IsClosed)
        {
            await SayAsync(session, Game18NConstString.ShipHasNotReturnedYet);
            return;
        }
        if (player.Gold < route.Fare)
        {
            await SayAsync(session, Game18NConstString.NotEnoughGold);
            return;
        }
        var departure = NextDeparture(clock.GetCurrentInstant(), route.DepartureIntervalSeconds);
        try
        {
            await mapChange.ChangeMapInstanceAsync(session, ship.MapInstanceId, route.ShipX, route.ShipY);
        }
        finally
        {
            if (session.HasPlayerEntity && session.Character.MapInstanceId == ship.MapInstanceId)
            {
                session.Character.BoardedShipId = ship.MapInstanceId;
                session.Character.ShipDepartureAt = departure;
                if (route.Fare > 0)
                {
                    session.Character.RemoveGold(route.Fare);
                }
            }
        }
        if (route.Fare > 0 && session.HasPlayerEntity && session.Character.MapInstanceId == ship.MapInstanceId)
        {
            await session.SendPacketAsync(session.Character.GenerateGold());
        }
    }

    public async Task CancelAsync(ClientSession session, NpcComponentBundle npc, NrunRunnerType runner, short option)
    {
        var ship = session.Character.MapInstance;
        var route = ship.ShipRoute;
        if (option != 0 || route == null || route.CancelRunner != runner
            || npc.VisualId != route.ShipRouteId || npc.VNum != route.CaptainVNum
            || npc.Dialog != route.CaptainDialog)
        {
            return;
        }
        await mapChange.ChangeMapAsync(session, route.SourceMapId, route.ReturnX, route.ReturnY);
        if (session.HasPlayerEntity && session.Character.MapInstanceId != ship.MapInstanceId)
        {
            session.Character.BoardedShipId = null;
        }
    }

    public async Task DepartAsync()
    {
        foreach (var session in sessions.GetSessions())
        {
            using (await session.AcquirePacketLockAsync())
            {
                if (!session.HasPlayerEntity || session.Character.BoardedShipId == null)
                {
                    continue;
                }
                var player = session.Character;
                var shipId = player.BoardedShipId;
                var route = player.MapInstance.ShipRoute;
                if (player.MapInstanceId != shipId || route == null)
                {
                    player.BoardedShipId = null;
                    continue;
                }
                if (player.IsDisconnecting || player.ShipDepartureAt > clock.GetCurrentInstant())
                {
                    continue;
                }
                try
                {
                    var changedFaction = route.RequiredFaction != null
                        && (byte)route.RequiredFaction != player.CharacterDto.Faction;
                    await mapChange.ChangeMapAsync(session,
                        changedFaction ? route.SourceMapId : route.DestinationMapId,
                        changedFaction ? route.ReturnX : route.DestinationX,
                        changedFaction ? route.ReturnY : route.DestinationY);
                    if (session.HasPlayerEntity && session.Character.MapInstanceId != shipId)
                    {
                        session.Character.BoardedShipId = null;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ship departure failed for character {CharacterId}", player.CharacterId);
                }
            }
        }
    }

    private static Task SayAsync(ClientSession session, Game18NConstString message) =>
        session.SendPacketAsync(new SayiPacket
        {
            VisualType = VisualType.Player,
            VisualId = session.Character.CharacterId,
            Type = SayColorType.Yellow,
            Message = message,
        });
}

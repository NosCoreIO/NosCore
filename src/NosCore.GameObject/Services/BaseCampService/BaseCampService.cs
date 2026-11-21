//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BaseCampService;

// Each side may keep one base camp per channel, planted with its flag anywhere in Glacernon.
// Its players can teleport to it once it is built; its enemies can tear it down.
public sealed class BaseCampService(IClock clock, ISessionRegistry sessionRegistry,
    IMapInstanceAccessorService mapInstanceAccessorService, INpcMonsterCatalog npcMonsters,
    IAggroService aggroService) : IBaseCampService, ISingletonService
{
    public static readonly Duration ConstructionTime = Duration.FromMinutes(30);

    private readonly Lock _lock = new();
    private readonly Dictionary<FactionType, Camp> _camps = new();

    public bool HasCamp(FactionType faction)
    {
        lock (_lock)
        {
            return _camps.ContainsKey(faction);
        }
    }

    public (Guid MapInstanceId, short X, short Y)? FinishedCamp(FactionType faction)
    {
        var now = clock.GetCurrentInstant();
        lock (_lock)
        {
            return _camps.TryGetValue(faction, out var camp) && camp.BuiltAt <= now
                ? (camp.MapInstanceId, camp.X, camp.Y)
                : null;
        }
    }

    public async Task BuildAsync(PlayerComponentBundle builder, short campVNum)
    {
        if (builder.MapInstance is not { } map || npcMonsters.GetNpcMonster(campVNum) is not { } campMonster)
        {
            return;
        }

        var faction = builder.Faction;
        var building = map.SpawnMonster(campMonster, builder.PositionX, builder.PositionY, false, false);
        building.Faction = faction;
        Camp? replaced;
        lock (_lock)
        {
            _camps.TryGetValue(faction, out replaced);
            _camps[faction] = new Camp(map.MapInstanceId, building.VisualId, builder.PositionX, builder.PositionY,
                clock.GetCurrentInstant() + ConstructionTime);
        }

        if (replaced != null)
        {
            await TearDownAsync(replaced);
        }

        await map.SendPacketAsync(building.GenerateIn());
        var message = faction == FactionType.Angel ? Game18NConstString.AngelCampBuilt : Game18NConstString.DevilCampBuilt;
        await Task.WhenAll(Act4Players().Select(p => p.SendPacketAsync(new MsgiPacket
        {
            Type = MessageType.Default,
            Message = message,
        })));
    }

    public bool CampFell(MonsterComponentBundle monster)
    {
        lock (_lock)
        {
            var fallen = _camps.FirstOrDefault(c => c.Value.MapInstanceId == monster.MapInstance.MapInstanceId
                && c.Value.MonsterId == monster.VisualId);
            return fallen.Value != null && _camps.Remove(fallen.Key);
        }
    }

    private async Task TearDownAsync(Camp camp)
    {
        if (mapInstanceAccessorService.GetMapInstance(camp.MapInstanceId) is not { } map
            || map.GetMonsterById((int)camp.MonsterId) is not { IsAlive: true } building)
        {
            return;
        }

        building.IsAlive = false;
        aggroService.Clear(building);
        await map.SendPacketAsync(new OutPacket { VisualType = VisualType.Monster, VisualId = building.VisualId });
    }

    private IEnumerable<PlayerComponentBundle> Act4Players() => sessionRegistry.GetCharacters(p => p.MapInstance is { IsAct4: true });

    private sealed record Camp(Guid MapInstanceId, long MonsterId, short X, short Y, Instant BuiltAt);
}

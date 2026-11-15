//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FactionWarService;

// Each channel runs its own war. Every enemy player killed on Act 4 moves the killer's side one
// percent closer to its raid. At 100% Lord Mukraju appears in that side's camp, and killing him
// within five minutes opens one of the four Glacernon raids to that side for an hour.
public sealed class FactionWarService(IClock clock, ISessionRegistry sessionRegistry,
    IMapInstanceAccessorService mapInstanceAccessorService, List<NpcMonsterDto> npcMonsters,
    IAggroService aggroService, IRandomProvider random) : IFactionWarService, ISingletonService
{
    public const int FullProgress = 10000;
    public const short Mukraju = 556;
    public const short RaidGateMapId = 134;
    public const short RaidGateX = 139;
    public const short RaidGateY = 99;
    public static readonly Duration GuardianTime = Duration.FromMinutes(5);
    public static readonly Duration RaidTime = Duration.FromHours(1);

    private const int ProgressPerKill = 100;
    private const int PointsPerKill = 2;
    private const int PointsLostPerDeath = 1;
    private const int PointsLevelRange = 10;

    private static readonly Act4RaidType[] Raids = Enum.GetValues<Act4RaidType>();

    private readonly Lock _lock = new();
    private readonly Side _angel = new(FactionType.Angel, 132, 114, 92, Game18NConstString.LordMukrajuHasAppearedInThe, PortalType.BlueRaid);
    private readonly Side _demon = new(FactionType.Demon, 133, 20, 95, Game18NConstString.LordMukrajuHasAppearedInThe2, PortalType.DarkRaid);

    public int Progress(FactionType faction)
    {
        lock (_lock)
        {
            return SideOf(faction)?.Progress ?? 0;
        }
    }

    public Act4WarMode Mode(FactionType faction)
    {
        lock (_lock)
        {
            return SideOf(faction)?.Mode ?? Act4WarMode.Gathering;
        }
    }

    public Act4RaidType? OpenRaid(FactionType faction)
    {
        lock (_lock)
        {
            return SideOf(faction) is { Mode: Act4WarMode.RaidOpen } side ? side.Raid : null;
        }
    }

    public Instant? RaidClosesAt(FactionType faction)
    {
        lock (_lock)
        {
            return SideOf(faction) is { Mode: Act4WarMode.RaidOpen } side ? side.EndsAt : null;
        }
    }

    public FcPacket GenerateFc(PlayerComponentBundle player)
    {
        var now = clock.GetCurrentInstant();
        lock (_lock)
        {
            return new FcPacket
            {
                Faction = player.Faction,
                MinutesUntilReset = MinutesUntilReset(now),
                Angel = _angel.GenerateFcSubPacket(now),
                Demon = _demon.GenerateFcSubPacket(now),
            };
        }
    }

    public GpPacket? GenerateRaidGate(PlayerComponentBundle player)
    {
        lock (_lock)
        {
            return player.MapInstance?.Map.MapId == RaidGateMapId && SideOf(player.Faction) is { Mode: Act4WarMode.RaidOpen } side
                ? side.GenerateGate(isDisabled: false)
                : null;
        }
    }

    public async Task<bool> RecordKillAsync(PlayerComponentBundle killer, PlayerComponentBundle victim)
    {
        if (killer.Faction == FactionType.Neutral || killer.Faction == victim.Faction)
        {
            return false;
        }

        Side? guardianCalled = null;
        lock (_lock)
        {
            if (SideOf(killer.Faction) is { Mode: Act4WarMode.Gathering } side)
            {
                side.Progress = Math.Min(FullProgress, side.Progress + ProgressPerKill);
                if (side.Progress == FullProgress)
                {
                    side.Progress = 0;
                    side.Mode = Act4WarMode.Guardian;
                    side.EndsAt = clock.GetCurrentInstant() + GuardianTime;
                    guardianCalled = side;
                }
            }
        }

        killer.CharacterDto.Act4Kill++;
        victim.CharacterDto.Act4Dead++;
        victim.CharacterDto.Act4Points = Math.Max(0, victim.CharacterDto.Act4Points - PointsLostPerDeath);
        if (Math.Abs(killer.Level - victim.Level) <= PointsLevelRange)
        {
            killer.CharacterDto.Act4Points += PointsPerKill;
        }

        var killerName = killer.Name;
        var victimName = victim.Name;
        var killerFaction = killer.Faction;
        await Task.WhenAll(Act4Players().Select(p => p.SendPacketAsync(p.GenerateSayi2(
            p.Faction == killerFaction ? SayColorType.Green : SayColorType.Red,
            Game18NConstString.PlayerHasDefeteadPlayerX, 2, killerName, victimName))));

        if (guardianCalled != null)
        {
            await SummonGuardianAsync(guardianCalled);
        }

        await RefreshAsync();
        return true;
    }

    public async Task<bool> GuardianDiedAsync(MonsterComponentBundle monster)
    {
        Side? opened;
        lock (_lock)
        {
            opened = new[] { _angel, _demon }.FirstOrDefault(s => s.Mode == Act4WarMode.Guardian
                && s.Guardian is { } guardian && guardian.MapInstanceId == monster.MapInstance.MapInstanceId
                && guardian.MonsterId == monster.VisualId);
            if (opened == null)
            {
                return false;
            }

            opened.Guardian = null;
            opened.Mode = Act4WarMode.RaidOpen;
            opened.Raid = Raids[random.Next(0, Raids.Length)];
            opened.EndsAt = clock.GetCurrentInstant() + RaidTime;
        }

        await SendGateAsync(opened, isDisabled: false);
        await RefreshAsync();
        return true;
    }

    public async Task TickAsync()
    {
        var now = clock.GetCurrentInstant();
        var ended = new List<(Side Side, Act4WarMode Mode, (Guid MapInstanceId, long MonsterId)? Guardian)>();
        lock (_lock)
        {
            foreach (var side in new[] { _angel, _demon })
            {
                if (side.Mode == Act4WarMode.Gathering || side.EndsAt > now)
                {
                    continue;
                }

                ended.Add((side, side.Mode, side.Guardian));
                side.Mode = Act4WarMode.Gathering;
                side.Guardian = null;
                side.Raid = null;
                side.EndsAt = null;
            }
        }

        if (ended.Count == 0)
        {
            return;
        }

        foreach (var (side, mode, guardian) in ended)
        {
            if (mode == Act4WarMode.RaidOpen)
            {
                await SendGateAsync(side, isDisabled: true);
            }
            else if (guardian is { } left)
            {
                await DismissGuardianAsync(left.MapInstanceId, left.MonsterId);
            }
        }

        await RefreshAsync();
    }

    private async Task SummonGuardianAsync(Side side)
    {
        if (mapInstanceAccessorService.GetBaseMapById(side.CampMapId) is not { } camp
            || npcMonsters.Find(m => m.NpcMonsterVNum == Mukraju) is not { } mukraju)
        {
            return;
        }

        var guardian = camp.SpawnMonster(mukraju, side.CampX, side.CampY, true);
        lock (_lock)
        {
            side.Guardian = (camp.MapInstanceId, guardian.VisualId);
        }

        await camp.SendPacketAsync(guardian.GenerateIn());
        await Task.WhenAll(Act4Players().Select(p => p.SendPacketAsync(new MsgiPacket
        {
            Type = MessageType.Default,
            Message = side.GuardianMessage,
        })));
    }

    private async Task DismissGuardianAsync(Guid mapInstanceId, long monsterId)
    {
        if (mapInstanceAccessorService.GetMapInstance(mapInstanceId) is not { } camp
            || camp.GetMonsterById((int)monsterId) is not { IsAlive: true } guardian)
        {
            return;
        }

        guardian.IsAlive = false;
        aggroService.Clear(guardian);
        await camp.SendPacketAsync(new OutPacket { VisualType = VisualType.Monster, VisualId = guardian.VisualId });
    }

    private Task SendGateAsync(Side side, bool isDisabled)
    {
        GpPacket gate;
        lock (_lock)
        {
            gate = side.GenerateGate(isDisabled);
        }

        return Task.WhenAll(sessionRegistry
            .GetCharacters(p => p.Faction == side.Faction && p.MapInstance is { MapInstanceType: Data.Enumerations.Map.MapInstanceType.BaseMapInstance } map
                && map.Map.MapId == RaidGateMapId)
            .Select(p => p.SendPacketAsync(gate)));
    }

    private Task RefreshAsync() => Task.WhenAll(Act4Players().Select(p => p.SendPacketAsync(GenerateFc(p))));

    private IEnumerable<PlayerComponentBundle> Act4Players() => sessionRegistry.GetCharacters(p => p.MapInstance is { IsAct4: true });

    private Side? SideOf(FactionType faction) => faction switch
    {
        FactionType.Angel => _angel,
        FactionType.Demon => _demon,
        _ => null,
    };

    private static int MinutesUntilReset(Instant now)
    {
        var today = now.InUtc().Date;
        var reset = new LocalDate(today.Year, today.Month, 1).PlusMonths(1).AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
        return (int)(reset - now).TotalMinutes;
    }

    private sealed class Side(FactionType faction, short campMapId, short campX, short campY,
        Game18NConstString guardianMessage, PortalType gateType)
    {
        public FactionType Faction { get; } = faction;
        public short CampMapId { get; } = campMapId;
        public short CampX { get; } = campX;
        public short CampY { get; } = campY;
        public Game18NConstString GuardianMessage { get; } = guardianMessage;

        public int Progress { get; set; }
        public Act4WarMode Mode { get; set; } = Act4WarMode.Gathering;
        public Instant? EndsAt { get; set; }
        public Act4RaidType? Raid { get; set; }
        public (Guid MapInstanceId, long MonsterId)? Guardian { get; set; }

        public FcSubPacket GenerateFcSubPacket(Instant now) => new()
        {
            Percentage = (byte)(Progress / 100),
            Mode = Mode,
            SecondsLeft = EndsAt is { } endsAt ? (short)Math.Max(0, (endsAt - now).TotalSeconds) : (short)0,
            TotalSeconds = Mode switch
            {
                Act4WarMode.Guardian => (short)GuardianTime.TotalSeconds,
                Act4WarMode.RaidOpen => (short)RaidTime.TotalSeconds,
                _ => 0,
            },
            IsMorcos = Raid == Act4RaidType.Morcos,
            IsHatus = Raid == Act4RaidType.Hatus,
            IsCalvina = Raid == Act4RaidType.Calvina,
            IsBerios = Raid == Act4RaidType.Berios,
        };

        // Both sides' gates stand on the same spot; each player is only shown their own side's.
        public GpPacket GenerateGate(bool isDisabled) => new()
        {
            SourceX = RaidGateX,
            SourceY = RaidGateY,
            PortalType = gateType,
            PortalId = (int)(Raid ?? 0),
            IsDisabled = isDisabled,
        };
    }
}

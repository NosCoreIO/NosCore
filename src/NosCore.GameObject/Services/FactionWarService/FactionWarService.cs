//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Families;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FactionWarService;

// Each channel runs its own war: every enemy player killed on Act 4 moves the killer's side one
// percent closer to its raid.
public sealed class FactionWarService(IClock clock, ISessionRegistry sessionRegistry) : IFactionWarService, ISingletonService
{
    public const int FullProgress = 10000;
    private const int ProgressPerKill = 100;
    private const int PointsPerKill = 2;
    private const int PointsLostPerDeath = 1;
    private const int PointsLevelRange = 10;

    private readonly Lock _lock = new();
    private int _angel;
    private int _demon;

    public int Progress(FactionType faction)
    {
        lock (_lock)
        {
            return faction switch
            {
                FactionType.Angel => _angel,
                FactionType.Demon => _demon,
                _ => 0,
            };
        }
    }

    public FcPacket GenerateFc(PlayerComponentBundle player) => new()
    {
        Faction = player.Faction,
        MinutesUntilReset = MinutesUntilReset(),
        Angel = Side(Progress(FactionType.Angel)),
        Demon = Side(Progress(FactionType.Demon)),
    };

    public async Task RecordKillAsync(PlayerComponentBundle killer, PlayerComponentBundle victim)
    {
        if (killer.Faction == FactionType.Neutral || killer.Faction == victim.Faction)
        {
            return;
        }

        lock (_lock)
        {
            if (killer.Faction == FactionType.Angel)
            {
                _angel = Math.Min(FullProgress, _angel + ProgressPerKill);
            }
            else
            {
                _demon = Math.Min(FullProgress, _demon + ProgressPerKill);
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
        await Task.WhenAll(sessionRegistry.GetCharacters(p => p.MapInstance is { IsAct4: true }).Select(async p =>
        {
            await p.SendPacketAsync(p.GenerateSayi2(p.Faction == killerFaction ? SayColorType.Green : SayColorType.Red,
                Game18NConstString.PlayerHasDefeteadPlayerX, 2, killerName, victimName));
            await p.SendPacketAsync(GenerateFc(p));
        }));
    }

    private static FcSubPacket Side(int progress) => new()
    {
        Percentage = (byte)(progress / 100),
        Mode = Act4WarMode.Gathering,
    };

    private int MinutesUntilReset()
    {
        var now = clock.GetCurrentInstant();
        var today = now.InUtc().Date;
        var reset = new LocalDate(today.Year, today.Month, 1).PlusMonths(1).AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
        return (int)(reset - now).TotalMinutes;
    }
}

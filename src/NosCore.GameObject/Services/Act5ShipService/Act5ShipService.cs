//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.Act5ShipService
{
    // The free ship to the Mortaz Desert sails every two minutes. Passengers wait on board until
    // the next departure; what happens aboard is the act5-ship script.
    public class Act5ShipService(
        IScriptedInstanceService scriptedInstanceService,
        IMapChangeService mapChangeService,
        IMapInstanceAccessorService mapInstanceAccessorService,
        IRandomProvider random,
        IClock clock) : IAct5ShipService, ISingletonService
    {
        public const string Script = "act5-ship";

        public static readonly Duration SailsEvery = Duration.FromMinutes(2);

        private static readonly (short X, short Y, short Width, short Height) DeckArea = (30, 22, 6, 10);

        private static readonly (short MapId, short X, short Y) Landing = (170, 127, 47);

        private const short LandingSize = 3;

        private readonly ConcurrentDictionary<Instant, ScriptedInstanceRun> _sailings = new();
        private readonly SemaphoreSlim _sailingLock = new(1, 1);
        private IInstanceScript? _script;

        public async Task BoardAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.MapInstance?.MapInstanceType != MapInstanceType.BaseMapInstance || !character.IsAlive)
            {
                return;
            }

            if (await GetOrCreateDeckAsync(NextDeparture()) is not { } deck
                || mapInstanceAccessorService.GetMapInstance(deck) is not { } ship)
            {
                return;
            }

            var (x, y) = FreeSpot(ship, DeckArea);
            await mapChangeService.ChangeMapInstanceAsync(session, deck, x, y);
        }

        public Task DisembarkAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.MapInstance is not { } ship || !_sailings.Values.Any(run => run.Rooms.Values.Contains(ship.MapInstanceId)))
            {
                return Task.CompletedTask;
            }

            return mapChangeService.ChangeMapAsync(session, character.MapId, character.MapX, character.MapY);
        }

        private Instant NextDeparture()
        {
            var now = clock.GetCurrentInstant();
            var period = SailsEvery.BclCompatibleTicks;
            var ticks = now.ToUnixTimeTicks();
            return Instant.FromUnixTimeTicks((ticks / period + 1) * period);
        }

        private async Task SailAsync(ClientSession session, EndStateType? end)
        {
            var character = session.Character;
            if (end != EndStateType.Success)
            {
                await mapChangeService.ChangeMapAsync(session, character.MapId, character.MapX, character.MapY);
                return;
            }

            var (x, y) = mapInstanceAccessorService.GetBaseMapById(Landing.MapId) is { } harbour
                ? FreeSpot(harbour, (Landing.X, Landing.Y, LandingSize, LandingSize))
                : (Landing.X, Landing.Y);
            await mapChangeService.ChangeMapAsync(session, Landing.MapId, x, y);
        }

        private (short X, short Y) FreeSpot(MapInstance map, (short X, short Y, short Width, short Height) area)
        {
            var cells = new List<(short X, short Y)>();
            for (var x = area.X; x < area.X + area.Width; x++)
            {
                for (var y = area.Y; y < area.Y + area.Height; y++)
                {
                    if (map.Map.IsWalkable(x, y))
                    {
                        cells.Add((x, y));
                    }
                }
            }

            return cells.Count == 0 ? (area.X, area.Y) : cells[random.Next(0, cells.Count)];
        }

        private async Task<Guid?> GetOrCreateDeckAsync(Instant departure)
        {
            await _sailingLock.WaitAsync();
            try
            {
                foreach (var sailed in _sailings.Where(s => s.Value.EndState != null).Select(s => s.Key).ToList())
                {
                    _sailings.TryRemove(sailed, out _);
                }

                if (_sailings.TryGetValue(departure, out var existing) && Deck(existing) is { } open)
                {
                    return open;
                }

                _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
                if (_script is not { } script)
                {
                    return null;
                }

                var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.NormalInstance,
                    new ShipHost(this, clock, departure - SailsEvery));
                if (run == null)
                {
                    return null;
                }

                _sailings[departure] = run;
                return Deck(run);
            }
            finally
            {
                _sailingLock.Release();
            }
        }

        private Guid? Deck(ScriptedInstanceRun run)
        {
            var room = run.Rooms.Values.First();
            return run.EndState == null && scriptedInstanceService.GetRun(room) == run ? room : null;
        }

        private sealed class ShipHost(Act5ShipService service, IClock clock, Instant openedAt) : IInstanceRunHost
        {
            private EndStateType? _end;

            public string Name => Script;

            public TimeSpan AlreadyElapsed => (clock.GetCurrentInstant() - openedAt).ToTimeSpan();

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
            {
                _end = state;
                return Task.CompletedTask;
            }

            public Task ReturnAsync(ClientSession session) => service.SailAsync(session, _end);
        }
    }
}

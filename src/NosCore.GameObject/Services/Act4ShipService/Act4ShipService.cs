//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ChannelService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.Act4ShipService
{
    // The ship takes passengers for twenty minutes, sails five minutes after boarding closes and
    // charges the fare as it leaves. Angels and demons wait on decks of their own; what happens
    // aboard is the act4-ship script.
    public class Act4ShipService(
        IScriptedInstanceService scriptedInstanceService,
        IMapChangeService mapChangeService,
        IChannelService channelService,
        IMapInstanceAccessorService mapInstanceAccessorService,
        IRandomProvider random,
        IClock clock,
        ILogger<Act4ShipService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IAct4ShipService, ISingletonService
    {
        public const string Script = "act4-ship";
        public const long Fare = 50000;

        public static readonly TimeSpan BoardingOpenFor = TimeSpan.FromMinutes(20);

        // From the moment boarding opens to the moment the ship sails.
        public static readonly TimeSpan CountdownLength = TimeSpan.FromMinutes(25);

        private static readonly (short X, short Y, short Width, short Height) DeckArea = (28, 22, 7, 13);

        private static readonly Dictionary<FactionType, (short MapId, short X, short Y)> Landing = new()
        {
            [FactionType.Angel] = (130, 10, 36),
            [FactionType.Demon] = (131, 10, 36),
        };

        private const short LandingSize = 5;

        private readonly ConcurrentDictionary<FactionType, ScriptedInstanceRun> _decks = new();
        private readonly SemaphoreSlim _deckLock = new(1, 1);
        private IInstanceScript? _script;
        private Instant _openedAt;
        private int _boarding;

        public bool IsBoarding => Volatile.Read(ref _boarding) == 1;

        public bool TryStart()
        {
            if (Interlocked.CompareExchange(ref _boarding, 1, 0) != 0)
            {
                return false;
            }

            _openedAt = clock.GetCurrentInstant();
            _decks.Clear();
            _ = Task.Run(async () =>
            {
                await Task.Delay(BoardingOpenFor);
                Volatile.Write(ref _boarding, 0);
            });
            return true;
        }

        public async Task BoardAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.MapInstance?.MapInstanceType != MapInstanceType.BaseMapInstance || !character.IsAlive)
            {
                return;
            }

            if (!Landing.ContainsKey(character.Faction))
            {
                await character.SendPacketAsync(Message(Game18NConstString.FactionRequired));
                return;
            }

            if (!IsBoarding)
            {
                await character.SendPacketAsync(Message(Game18NConstString.ShipHasNotReturnedYet));
                return;
            }

            if (character.Gold < Fare)
            {
                await character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughGold));
                return;
            }

            if (await GetOrCreateDeckAsync(character.Faction) is not { } deck
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
            if (character.MapInstance is not { } ship || !_decks.Values.Any(run => run.Rooms.Values.Contains(ship.MapInstanceId)))
            {
                return Task.CompletedTask;
            }

            return mapChangeService.ChangeMapAsync(session, character.MapId, character.MapX, character.MapY);
        }

        // Passengers who can no longer pay, or have lost their side, are put ashore where they boarded.
        private async Task SailAsync(ClientSession session, EndStateType? end)
        {
            var character = session.Character;
            if (end != EndStateType.Success || !Landing.TryGetValue(character.Faction, out var landing) || character.Gold < Fare)
            {
                if (end == EndStateType.Success && character.Gold < Fare)
                {
                    await character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughGold));
                }

                await mapChangeService.ChangeMapAsync(session, character.MapId, character.MapX, character.MapY);
                return;
            }

            character.RemoveGold(Fare);
            await character.SendPacketAsync(character.GenerateGold());
            var (x, y) = mapInstanceAccessorService.GetBaseMapById(landing.MapId) is { } citadel
                ? FreeSpot(citadel, (landing.X, landing.Y, LandingSize, LandingSize))
                : (landing.X, landing.Y);
            if (await channelService.GetChannelHostingAsync(true) is { } act4Channel)
            {
                await channelService.MoveToMapAsync(session, act4Channel, ItModeType.ShipToAct4, landing.MapId, x, y);
                return;
            }

            await mapChangeService.ChangeMapAsync(session, landing.MapId, x, y);
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

        private async Task<Guid?> GetOrCreateDeckAsync(FactionType faction)
        {
            await _deckLock.WaitAsync();
            try
            {
                if (_decks.TryGetValue(faction, out var existing) && Deck(existing) is { } open)
                {
                    return open;
                }

                _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
                if (_script is not { } script)
                {
                    logger.LogError(logLanguage[LogLanguageKey.ACT4_SHIP_SCRIPT_MISSING], Script);
                    return null;
                }

                var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.NormalInstance,
                    new ShipHost(this, clock, _openedAt));
                if (run == null)
                {
                    return null;
                }

                _decks[faction] = run;
                return Deck(run);
            }
            finally
            {
                _deckLock.Release();
            }
        }

        private Guid? Deck(ScriptedInstanceRun run)
        {
            var room = run.Rooms.Values.First();
            return run.EndState == null && scriptedInstanceService.GetRun(room) == run ? room : null;
        }

        private static MsgiPacket Message(Game18NConstString message) => new() { Type = MessageType.Default, Message = message };

        private sealed class ShipHost(Act4ShipService service, IClock clock, Instant openedAt) : IInstanceRunHost
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

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FactionWarService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.Act4RaidService
{
    // While a side's raid is open, each family of that side gets its own run of it through the gate
    // on the Glacernon raid map. A family that has beaten the boss waits for the next opening.
    public class Act4RaidService(
        IFactionWarService factionWarService,
        IScriptedInstanceService scriptedInstanceService,
        IMapChangeService mapChangeService,
        IItemGenerationService itemProvider,
        IClock clock,
        ILogger<Act4RaidService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IAct4RaidService, ISingletonService
    {
        public const int Reputation = 10000;
        public const short ReturnX = 139;
        public const short ReturnY = 100;
        private const int GateReach = 2;
        private static readonly Duration ReviveDelay = Duration.FromSeconds(20);
        private static readonly Duration UrgentFrom = Duration.FromMinutes(30);

        private readonly ConcurrentDictionary<(long FamilyId, FactionType Faction), ScriptedInstanceRun> _runs = new();
        private readonly ConcurrentDictionary<Act4RaidType, IInstanceScript> _scripts = new();
        private readonly SemaphoreSlim _runLock = new(1, 1);

        public static string Script(Act4RaidType raid) => $"act4-{raid.ToString().ToLowerInvariant()}";

        public async Task<bool> UseGateAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.MapInstance is not { MapInstanceType: MapInstanceType.BaseMapInstance } map
                || map.Map.MapId != FactionWarService.FactionWarService.RaidGateMapId
                || Math.Abs(character.PositionX - FactionWarService.FactionWarService.RaidGateX) > GateReach
                || Math.Abs(character.PositionY - FactionWarService.FactionWarService.RaidGateY) > GateReach
                || factionWarService.OpenRaid(character.Faction) is not { } raid
                || factionWarService.RaidClosesAt(character.Faction) is not { } closesAt)
            {
                return false;
            }

            if (character.Family is not { } family)
            {
                await character.SendPacketAsync(Message(Game18NConstString.NotAuthorised));
                return true;
            }

            if (await LoadScriptAsync(raid) is not { } script)
            {
                return true;
            }

            if (character.Level < script.Definition.LevelMinimum)
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.TooLowLevelToEntry,
                    ArgumentType = 4,
                    Game18NArguments = { script.Definition.LevelMinimum },
                });
                return true;
            }

            var openedAt = closesAt - FactionWarService.FactionWarService.RaidTime;
            if (await GetOrCreateRunAsync(family.FamilyId, character.Faction, raid, script, openedAt) is not { } room)
            {
                await character.SendPacketAsync(Message(Game18NConstString.PortalHasClosed));
                return true;
            }

            await mapChangeService.ChangeMapInstanceAsync(session, room, script.Definition.StartX, script.Definition.StartY);
            var left = closesAt - clock.GetCurrentInstant();
            await character.SendPacketAsync(new DgPacket
            {
                RaidType = (byte)raid,
                Urgency = (byte)(left > UrgentFrom ? 1 : 2),
                Seconds = (int)Math.Max(0, left.TotalSeconds),
            });
            return true;
        }

        private async Task<IInstanceScript?> LoadScriptAsync(Act4RaidType raid)
        {
            if (_scripts.TryGetValue(raid, out var loaded))
            {
                return loaded;
            }

            if (await scriptedInstanceService.LoadScriptAsync(Script(raid)) is not { } script)
            {
                logger.LogError(logLanguage[LogLanguageKey.ACT4_RAID_SCRIPT_MISSING], Script(raid));
                return null;
            }

            return _scripts.GetOrAdd(raid, script);
        }

        // A run whose last player has left is closed and the family's next arrival opens a new one,
        // unless the family has already beaten this opening's boss.
        private async Task<Guid?> GetOrCreateRunAsync(long familyId, FactionType faction, Act4RaidType raid,
            IInstanceScript script, Instant openedAt)
        {
            await _runLock.WaitAsync();
            try
            {
                if (_runs.TryGetValue((familyId, faction), out var existing) && existing.Host is RaidHost host
                    && host.OpenedAt == openedAt)
                {
                    if (existing.EndState == EndStateType.Success)
                    {
                        return null;
                    }

                    var room = existing.Rooms.Values.First();
                    if (existing.EndState == null && scriptedInstanceService.GetRun(room) == existing)
                    {
                        return room;
                    }
                }

                var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.Act4Instance,
                    new RaidHost(this, clock, raid, openedAt, script.Definition));
                if (run == null)
                {
                    return null;
                }

                _runs[(familyId, faction)] = run;
                return run.Rooms.Values.First();
            }
            finally
            {
                _runLock.Release();
            }
        }

        private async Task RewardAsync(ScriptedInstanceRun run, ScriptedInstanceDefinition definition)
        {
            foreach (var session in run.Players().Where(s => s.HasPlayerEntity).ToList())
            {
                var character = session.Character;
                await character.SetReputationAsync(character.Reputation + Reputation);
                foreach (var gift in definition.GiftItems)
                {
                    var item = itemProvider.Create(gift.VNum, gift.Amount, 0, 0, gift.Design);
                    if (character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, character.CharacterId))?.FirstOrDefault() is { } added)
                    {
                        await session.SendPacketAsync(added.GeneratePocketChange((PocketType)added.Type, added.Slot));
                        await session.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                            2, gift.VNum.ToString(), gift.Amount));
                        continue;
                    }

                    logger.LogWarning(logLanguage[LogLanguageKey.RAID_REWARD_NO_ROOM], (int)definition.Id, gift.VNum, character.CharacterId);
                    await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                }
            }
        }

        private async Task ReviveDueAsync(RaidHost host, ScriptedInstanceRun run)
        {
            var now = clock.GetCurrentInstant();
            foreach (var (characterId, at) in host.RevivesAt.Where(s => s.Value <= now).ToList())
            {
                host.RevivesAt.TryRemove(characterId, out _);
                if (run.Players().FirstOrDefault(s => s.HasPlayerEntity && s.Character.CharacterId == characterId) is { } session)
                {
                    run.StoodUp(characterId);
                    await session.Character.ReviveAsync(fully: true);
                }
            }
        }

        private async Task ReturnAsync(ClientSession session)
        {
            var player = session.Character;
            if (!player.IsAlive)
            {
                await player.ReviveAsync(fully: false);
            }

            await mapChangeService.ChangeMapAsync(session, FactionWarService.FactionWarService.RaidGateMapId, ReturnX, ReturnY);
        }

        private static MsgiPacket Message(Game18NConstString message) => new() { Type = MessageType.Default, Message = message };

        private sealed class RaidHost(Act4RaidService service, IClock clock, Act4RaidType raid, Instant openedAt,
            ScriptedInstanceDefinition definition) : IInstanceRunHost
        {
            public Instant OpenedAt { get; } = openedAt;

            public ConcurrentDictionary<long, Instant> RevivesAt { get; } = new();

            public string Name => Script(raid);

            public TimeSpan AlreadyElapsed => (clock.GetCurrentInstant() - OpenedAt).ToTimeSpan();

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state) =>
                state == EndStateType.Success ? service.RewardAsync(run, definition) : Task.CompletedTask;

            public Task ReturnAsync(ClientSession session) => service.ReturnAsync(session);

            public Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player)
            {
                RevivesAt[player.CharacterId] = clock.GetCurrentInstant().Plus(ReviveDelay);
                return Task.CompletedTask;
            }

            public Task TickAsync(ScriptedInstanceRun run) => service.ReviveDueAsync(this, run);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosCore.Algorithm.InstantCombatRewardService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Instances.Abstractions;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using NosCore.Core.Observability;

namespace NosCore.GameObject.Services.InstantCombatService
{
    public class InstantCombatService(
        List<InstantCombatMonsterDto> monsters,
        List<InstantCombatDropDto> drops,
        IMapInstanceRegistry mapInstanceRegistry,
        IMapChangeService mapChangeService,
        IScriptedInstanceService scriptedInstanceService,
        ISessionRegistry sessionRegistry,
        IItemGenerationService itemGenerationService,
        IRandomProvider random,
        IInstantCombatRewardService rewardService,
        IFamilyContributionService familyContributionService,
        IOptions<WorldConfiguration> worldConfiguration,
        ILogger<InstantCombatService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, TimeProvider time) : IInstantCombatService, ISingletonService
    {
        public const short MapId = 2004;
        public const int PlayersPerRoom = 50;
        public const string Script = "instant-combat";

        private static readonly (TimeSpan Before, Game18NConstString Delay, int Amount)[] Countdown =
        [
            (TimeSpan.FromMinutes(5), Game18NConstString.InMinutes, 5),
            (TimeSpan.FromMinutes(1), Game18NConstString.InMinutes, 1),
            (TimeSpan.FromSeconds(30), Game18NConstString.InSeconds, 30),
            (TimeSpan.FromSeconds(10), Game18NConstString.InSeconds, 10),
        ];

        // The client formats the delay line with the first two arguments and puts it before the message.
        private const byte DelayThenMessage = 10;

        // The client shows the message followed by the text of the const string in the first argument.
        private const byte MessageThenReason = 11;

        private static readonly TimeSpan SignUpTime = TimeSpan.FromSeconds(30);

        // Who has not answered yet is shown the start again this often until the sign-up closes.
        private static readonly TimeSpan SignUpReminder = TimeSpan.FromSeconds(7);

        private const byte InstantCombatSignUpClosed = 4;

        private int _running;
        private IInstanceScript? _script;
        private volatile bool _signUpOpen;

        public bool IsRunning => Volatile.Read(ref _running) == 1;

        public static TimeSpan CountdownLength => Countdown[0].Before;

        public bool TryStart(bool withCountdown)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                return false;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await RunAsync(withCountdown);
                }
                catch (Exception e)
                {
                    logger.LogError(e, logLanguage[LogLanguageKey.INSTANT_COMBAT_FAILED]);
                    NosCoreMetrics.Failures.Add(1, new KeyValuePair<string, object?>("boundary", "event_run"));
                }
                finally
                {
                    _signUpOpen = false;
                    Volatile.Write(ref _running, 0);
                }
            });
            return true;
        }

        public void SignUp(ClientSession session)
        {
            if (!_signUpOpen || !session.HasPlayerEntity
                || session.Character.MapInstance?.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                return;
            }

            session.Character.IsWaitingForEvent = true;
        }

        public static IEnumerable<InstantCombatRoom> GroupIntoRooms(IEnumerable<ClientSession> players,
            IReadOnlyCollection<byte> brackets)
        {
            return players
                .GroupBy(p => brackets.Where(b => b <= p.Character.Level).DefaultIfEmpty(brackets.Min()).Max())
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderBy(p => p.Character.Level)
                    .Chunk(PlayersPerRoom)
                    .Select(chunk => new InstantCombatRoom(g.Key, chunk)));
        }

        private async Task RunAsync(bool withCountdown)
        {
            var brackets = monsters.Select(m => m.LevelMinimum).Distinct().ToList();
            _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
            if (_script is not { } script || brackets.Count == 0)
            {
                return;
            }

            if (withCountdown)
            {
                for (var i = 0; i < Countdown.Length; i++)
                {
                    var (before, delay, amount) = Countdown[i];
                    await AnnounceAsync(delay, amount);
                    await Task.Delay(i + 1 < Countdown.Length ? before - Countdown[i + 1].Before : before, time);
                }
            }

            var signUpOpened = time.GetUtcNow();
            await sessionRegistry.BroadcastPacketAsync(Started);
            _signUpOpen = true;
            await sessionRegistry.BroadcastPacketAsync(new QnamliPacket
            {
                Type = QnamlType.InstantCombat,
                YesPacket = new NosCore.Packets.ClientPackets.UI.GuriPacket { Type = GuriPacketType.EventInWaiting },
                Question = Game18NConstString.JoinInstantCombatQuestion,
            });
            for (var reminder = signUpOpened + SignUpReminder; reminder < signUpOpened + SignUpTime; reminder += SignUpReminder)
            {
                await DelayUntilAsync(reminder);
                foreach (var session in sessionRegistry.GetSessions(s => s.HasPlayerEntity && !s.Character.IsWaitingForEvent))
                {
                    await session.SendPacketAsync(Started);
                }
            }

            await DelayUntilAsync(signUpOpened + SignUpTime);
            _signUpOpen = false;

            var players = sessionRegistry.GetSessions(s => s.HasPlayerEntity).ToList();
            var joining = players.Where(s => s.Character.IsWaitingForEvent
                && s.Character.IsAlive
                && s.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance).ToList();
            foreach (var session in players)
            {
                if (!session.Character.IsWaitingForEvent)
                {
                    await session.SendPacketAsync(new EsfPacket { Value = InstantCombatSignUpClosed });
                }

                session.Character.IsWaitingForEvent = false;
            }

            foreach (var room in GroupIntoRooms(joining, brackets))
            {
                var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.InstantCombatInstance,
                    new RoomHost(this, room.LevelMinimum));
                if (run == null)
                {
                    return;
                }

                var arena = run.Rooms[script.Definition.Rooms[0].Key];
                var mapInstance = mapInstanceRegistry.GetById(arena);
                foreach (var player in room.Players)
                {
                    var (x, y) = mapInstance == null ? ((short)0, (short)0) : RandomCell(mapInstance);
                    await mapChangeService.ChangeMapInstanceAsync(player, arena, x, y);
                }
            }
        }

        private IEnumerable<WaveMonster> WaveMonsters(byte levelMinimum, byte wave) => monsters
            .Where(m => m.LevelMinimum == levelMinimum && m.Wave == wave)
            .Select(m => new WaveMonster(m.NpcMonsterVNum, m.Amount, m.IsMoving));

        public async Task GiveWavePrizeAsync(MapInstance room, byte levelMinimum, byte wave)
        {
            var prize = drops.Where(d => d.LevelMinimum == levelMinimum && d.Wave == wave).ToList();
            foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(room.MapInstanceId)
                         .Where(s => s.HasPlayerEntity).ToList())
            {
                var character = session.Character;
                foreach (var line in prize)
                {
                    var item = itemGenerationService.Create(line.ItemVNum, line.Amount);
                    if (character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, character.CharacterId)) is not { Count: > 0 } added)
                    {
                        await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                        continue;
                    }

                    foreach (var slot in added)
                    {
                        await session.SendPacketAsync(slot.GeneratePocketChange(slot.Type.ToPocketType(), slot.Slot));
                    }

                    await session.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                        2, line.ItemVNum.ToString(), line.Amount));
                }
            }
        }

        private async Task TimedOutAsync(ScriptedInstanceRun run)
        {
            foreach (var session in run.Players().ToList())
            {
                await session.SendPacketAsync(new EventPacket
                {
                    Type = EventPacketType.ClearTimeSpaceUI,
                    Parameter = -1,
                    SecondParameter = -1,
                });
                await ReturnAsync(session);
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.InstantCombatOver,
                    ArgumentType = MessageThenReason,
                    Game18NArguments = { (int)Game18NConstString.TimeLimitExceeded },
                });
            }
        }

        private async Task RewardSurvivorsAsync(ScriptedInstanceRun run)
        {
            foreach (var session in run.Players().Where(s => s.HasPlayerEntity && s.Character.IsAlive).ToList())
            {
                await RewardAsync(session.Character);
            }
        }

        private Task ReturnAsync(ClientSession session) =>
            mapChangeService.ChangeMapAsync(session, session.Character.MapId, session.Character.MapX,
                session.Character.MapY);

        private sealed class RoomHost(InstantCombatService service, byte levelMinimum) : IInstanceRunHost
        {
            public string Name => $"Instant Combat {levelMinimum}+";

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => service.WaveMonsters(levelMinimum, wave);

            public Task DropWaveAsync(MapInstance room, byte wave) => service.GiveWavePrizeAsync(room, levelMinimum, wave);

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state) => state switch
            {
                EndStateType.Success => service.RewardSurvivorsAsync(run),
                EndStateType.Fail => service.TimedOutAsync(run),
                _ => Task.CompletedTask,
            };

            public Task ReturnAsync(ClientSession session) => service.ReturnAsync(session);
        }

        private async Task RewardAsync(PlayerComponentBundle character)
        {
            var gold = rewardService.GetGold(character.Level);
            character.Gold = Math.Min(worldConfiguration.Value.MaxGoldAmount, character.Gold + gold);
            await character.SendPacketAsync(character.GenerateGold());
            await character.SetReputationAsync(character.Reputation + rewardService.GetReputation(character.Level));
            await character.SendPacketAsync(new MsgiPacket
            {
                Message = Game18NConstString.EarnedReputationAndReward,
                ArgumentType = 4,
                Game18NArguments = { gold },
            });
            if (character.Family is { } family)
            {
                await familyContributionService.InstantCombatWonAsync(family.FamilyId, character.CharacterId);
            }

            var spPoints = rewardService.GetSpAdditionPoints(character.Level);
            character.AddAdditionalSpPoints(spPoints, worldConfiguration);
            await character.SendPacketAsync(character.GenerateSpPoint(worldConfiguration));
            await character.SendPacketAsync(character.GenerateSayi(SayColorType.Green,
                Game18NConstString.ReceiveSpecialistPoints, 4, spPoints));
        }

        private (short X, short Y) RandomCell(MapInstance mapInstance)
        {
            return mapInstance.Map.RandomWalkableCell(random, 100) ?? (0, 0);
        }

        private static MsgiPacket Started => new() { Type = MessageType.Notification, Message = Game18NConstString.InstantCombatStarted };

        private async Task DelayUntilAsync(DateTimeOffset due)
        {
            var left = due - time.GetUtcNow();
            if (left > TimeSpan.Zero)
            {
                await Task.Delay(left, time);
            }
        }

        private async Task AnnounceAsync(Game18NConstString delay, int amount)
        {
            foreach (var type in new[] { MessageType.Notification, MessageType.Default })
            {
                await sessionRegistry.BroadcastPacketAsync(new MsgiPacket
                {
                    Type = type,
                    Message = Game18NConstString.InstantCombatWillBegin,
                    ArgumentType = DelayThenMessage,
                    Game18NArguments = { (int)delay, amount },
                });
            }
        }
    }
}

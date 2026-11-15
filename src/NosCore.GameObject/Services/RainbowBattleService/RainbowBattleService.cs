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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.RainbowBattle;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Services.RainbowBattleService
{
    public class RainbowBattleService(
        IScriptedInstanceService scriptedInstanceService,
        IMapInstanceRegistry mapInstanceRegistry,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IItemGenerationService itemGenerationService,
        IRandomProvider random,
        IClock clock,
        ILogger<RainbowBattleService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : IRainbowBattleService, ISingletonService
    {
        public const string Script = "rainbow-battle";
        public const short LargeCrystal = 922;
        public const short MediumCrystal = 923;
        public const short SmallCrystal = 924;

        public static readonly TimeSpan CaptureTime = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan RescueTime = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan FrozenTime = TimeSpan.FromSeconds(20);
        public static readonly TimeSpan ScoringInterval = TimeSpan.FromSeconds(10);

        private static readonly (TimeSpan Before, Game18NConstString Message, int Amount)[] Countdown =
        [
            (TimeSpan.FromMinutes(5), Game18NConstString.RainbowBattleStartInMinutes, 5),
            (TimeSpan.FromMinutes(1), Game18NConstString.RainbowBattleStartInMinutes, 1),
            (TimeSpan.FromSeconds(30), Game18NConstString.RainbowBattleWillStart, 30),
            (TimeSpan.FromSeconds(10), Game18NConstString.RainbowBattleWillStart, 10),
        ];

        private readonly ConcurrentDictionary<long, byte> _registrants = new();
        private int _running;
        private volatile bool _open;
        private Instant _startsAt;
        private IInstanceScript? _script;

        public bool IsRunning => Volatile.Read(ref _running) == 1;

        public static TimeSpan CountdownLength => Countdown[0].Before;

        public static int PointsOf(RainbowCrystalSize size) => size switch
        {
            RainbowCrystalSize.Large => 3,
            RainbowCrystalSize.Medium => 2,
            _ => 1,
        };

        public static RainbowCrystalSize? SizeOf(short vNum) => vNum switch
        {
            LargeCrystal => RainbowCrystalSize.Large,
            MediumCrystal => RainbowCrystalSize.Medium,
            SmallCrystal => RainbowCrystalSize.Small,
            _ => null,
        };

        // Sorted strongest first, the registrants are cut into as few battles as the size cap allows,
        // and each battle deals its players out red, blue, blue, red so both sides get the same strength.
        public static List<List<(long CharacterId, RainbowBattleTeamType Team)>> Split(
            IReadOnlyList<(long CharacterId, byte Level, byte HeroLevel)> players, int minimum, int maximum)
        {
            var battles = new List<List<(long, RainbowBattleTeamType)>>();
            if (players.Count < Math.Max(minimum, 2))
            {
                return battles;
            }

            var ordered = players.OrderByDescending(p => p.Level).ThenByDescending(p => p.HeroLevel).ToList();
            var count = maximum > 0 ? (ordered.Count + maximum - 1) / maximum : 1;
            var taken = 0;
            for (var battle = 0; battle < count; battle++)
            {
                var size = ordered.Count / count + (battle < ordered.Count % count ? 1 : 0);
                battles.Add(ordered.Skip(taken).Take(size)
                    .Select((p, i) => (p.CharacterId, i % 4 is 0 or 3 ? RainbowBattleTeamType.Red : RainbowBattleTeamType.Blue))
                    .ToList());
                taken += size;
            }

            return battles;
        }

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
                    logger.LogError(e, logLanguage[LogLanguageKey.RAINBOW_BATTLE_FAILED]);
                }
                finally
                {
                    _open = false;
                    _registrants.Clear();
                    Volatile.Write(ref _running, 0);
                }
            });
            return true;
        }

        public async Task RegisterAsync(ClientSession session)
        {
            if (!session.HasPlayerEntity || _script is not { } script)
            {
                return;
            }

            var player = session.Character;
            if (!_open)
            {
                if (IsRunning)
                {
                    await player.SendPacketAsync(Message(Game18NConstString.NoRequestWhileRainbowBattle));
                }

                return;
            }

            if (player.MapInstance?.MapInstanceType != MapInstanceType.BaseMapInstance || !player.IsAlive)
            {
                return;
            }

            if (player.Level < script.Definition.LevelMinimum)
            {
                await player.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.TooLowLevelToEntry,
                    ArgumentType = 4,
                    Game18NArguments = { script.Definition.LevelMinimum },
                });
                return;
            }

            if (!_registrants.TryAdd(player.CharacterId, 0))
            {
                return;
            }

            var left = Math.Max(0, (_startsAt - clock.GetCurrentInstant()).TotalSeconds);
            await player.SendPacketAsync(Search(BsInfoMode.Open, (short)left));
            await player.SendPacketAsync(Message(Game18NConstString.SignedToRainbowBattle));
        }

        public async Task CancelAsync(ClientSession session)
        {
            if (session.HasPlayerEntity && _registrants.TryRemove(session.Character.CharacterId, out _))
            {
                await session.SendPacketAsync(Search(BsInfoMode.Close, 0));
            }
        }

        public async Task StartCaptureAsync(PlayerComponentBundle player, NpcComponentBundle crystal)
        {
            if (Battle(player) is not { } battle || SizeOf(crystal.VNum) == null
                || player.RainbowBattleTeam is null || player.RainbowBattleThawsAt != null
                || !battle.Host.CanCapture(crystal.VisualId, player.RainbowBattleTeam.Value))
            {
                return;
            }

            await player.StartDelayAsync(new DelayPacket
            {
                Delay = (short)CaptureTime.TotalMilliseconds,
                Type = DelayPacketType.InUse,
                Packet = new ClientGuriPacket
                {
                    Type = GuriPacketType.RainbowBattleCapture,
                    Argument = (int)crystal.VisualId,
                },
            }, crystal.VisualId, clock.GetCurrentInstant());
        }

        public Task CompleteCaptureAsync(PlayerComponentBundle player, int crystalId)
        {
            if (Battle(player) is not { } battle || player.MapInstance.GetNpcById(crystalId) is not { } crystal
                || !player.TryCompleteDelay(DelayPacketType.InUse, crystal.VisualId, clock.GetCurrentInstant()))
            {
                return Task.CompletedTask;
            }

            return battle.Run.RunHostAsync(() => battle.Host.CaptureAsync(battle.Run, player, crystal));
        }

        public async Task StartRescueAsync(PlayerComponentBundle player, PlayerComponentBundle frozen)
        {
            if (Battle(player) is null || player.RainbowBattleTeam is null || player.RainbowBattleThawsAt != null
                || frozen.RainbowBattleTeam != player.RainbowBattleTeam || frozen.RainbowBattleThawsAt == null
                || frozen.MapInstance?.MapInstanceId != player.MapInstance?.MapInstanceId)
            {
                return;
            }

            await player.StartDelayAsync(new DelayPacket
            {
                Delay = (short)RescueTime.TotalMilliseconds,
                Type = DelayPacketType.UnFreeze,
                Packet = new ClientGuriPacket
                {
                    Type = GuriPacketType.RainbowBattleUnfreeze,
                    VisualId = frozen.CharacterId,
                },
            }, frozen.CharacterId, clock.GetCurrentInstant());
        }

        public Task CompleteRescueAsync(PlayerComponentBundle player, long frozenId)
        {
            if (Battle(player) is not { } battle
                || sessionRegistry.GetSessionByCharacterId(frozenId) is not { HasPlayerEntity: true } frozen
                || !player.TryCompleteDelay(DelayPacketType.UnFreeze, frozenId, clock.GetCurrentInstant()))
            {
                return Task.CompletedTask;
            }

            return battle.Run.RunHostAsync(() => battle.Host.RescueAsync(battle.Run, player, frozen.Character));
        }

        public async Task PlayerDownAsync(PlayerComponentBundle player, IAliveEntity? killer)
        {
            if (Battle(player) is not { } battle)
            {
                return;
            }

            if (killer is PlayerComponentBundle enemy && enemy.CharacterId != player.CharacterId && enemy.RainbowBattleTeam != null)
            {
                await battle.Run.RunHostAsync(() => battle.Host.DefeatedAsync(battle.Run, enemy));
            }

            await battle.Run.PlayerDownAsync(player);
        }

        private (ScriptedInstanceRun Run, RunHost Host)? Battle(PlayerComponentBundle player) =>
            player.MapInstance is { MapInstanceType: MapInstanceType.RainbowBattleInstance } room
            && scriptedInstanceService.GetRun(room.MapInstanceId) is { Host: RunHost host } run
                ? (run, host)
                : null;

        private async Task RunAsync(bool withCountdown)
        {
            _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
            if (_script is not { } script)
            {
                return;
            }

            var steps = withCountdown ? Countdown : Countdown[^1..];
            _registrants.Clear();
            _startsAt = clock.GetCurrentInstant().Plus(Duration.FromTimeSpan(steps[0].Before));
            _open = true;
            await BroadcastAsync(Game18NConstString.CanSignUpForTheRainbowBattleNow);
            foreach (var session in sessionRegistry.GetSessions(s => s.HasPlayerEntity
                         && s.Character.Level >= script.Definition.LevelMinimum
                         && s.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance))
            {
                await session.SendPacketAsync(new QnamliPacket
                {
                    Type = QnamlType.RainbowBattle,
                    YesPacket = new ClientGuriPacket { Type = GuriPacketType.RainbowBattleRegistration },
                    Question = Game18NConstString.WantToTakePartInTheRainbowBattle,
                });
            }

            for (var i = 0; i < steps.Length; i++)
            {
                var (before, message, amount) = steps[i];
                foreach (var type in new[] { MessageType.Default, MessageType.Notification })
                {
                    await sessionRegistry.BroadcastPacketAsync(new MsgiPacket
                    {
                        Type = type,
                        Message = message,
                        ArgumentType = 4,
                        Game18NArguments = { amount },
                    });
                }

                await Task.Delay(i + 1 < steps.Length ? before - steps[i + 1].Before : before);
            }

            _open = false;
            var registered = _registrants.Keys
                .Select(sessionRegistry.GetSessionByCharacterId)
                .OfType<ClientSession>()
                .Where(s => s.HasPlayerEntity)
                .ToList();
            foreach (var session in registered)
            {
                await session.SendPacketAsync(Search(BsInfoMode.Close, 0));
            }

            var ready = registered
                .Where(s => s.Character.IsAlive && s.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
                .ToList();
            var battles = Split(ready.Select(s => (s.Character.CharacterId, s.Character.Level, s.Character.HeroLevel)).ToList(),
                script.Definition.TeamSizeMinimum, script.Definition.TeamSizeMaximum);
            if (battles.Count == 0)
            {
                foreach (var session in registered)
                {
                    await session.SendPacketAsync(Message(Game18NConstString.NobodyReadyRainbowBattle));
                }

                return;
            }

            await BroadcastAsync(Game18NConstString.TheRainbowBattleIsStarting);
            foreach (var battle in battles)
            {
                await LaunchAsync(script, battle.Select(p => (ready.First(s => s.Character.CharacterId == p.CharacterId), p.Team)).ToList());
            }
        }

        private async Task LaunchAsync(IInstanceScript script, List<(ClientSession Session, RainbowBattleTeamType Team)> players)
        {
            var host = new RunHost(this, script.Definition);
            var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.RainbowBattleInstance, host);
            if (run == null || mapInstanceRegistry.GetById(run.Rooms[script.Definition.Rooms[0].Key]) is not { } room)
            {
                return;
            }

            foreach (var (session, team) in players)
            {
                var player = session.Character;
                host.Join(player.CharacterId, team);
                player.RainbowBattleTeam = team;
                player.RainbowBattleThawsAt = null;
                player.RainbowBattleKills = 0;
                player.Hp = player.MaxHp;
                player.Mp = player.MaxMp;
                var (x, y) = host.BaseCell(room, team);
                await mapChangeService.ChangeMapInstanceAsync(session, room.MapInstanceId, x, y);
            }

            foreach (var (session, team) in players)
            {
                await host.WelcomeAsync(session.Character, team);
            }

            await run.StartAsync();
        }

        private IRandomProvider Random => random;

        private IClock Clock => clock;

        private ClientSession? Session(long characterId) => sessionRegistry.GetSessionByCharacterId(characterId);

        private async Task RewardAsync(PlayerComponentBundle player, short vNum, short amount)
        {
            var item = itemGenerationService.Create(vNum, amount);
            var added = player.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, player.CharacterId));
            if (added is not { Count: > 0 })
            {
                await player.SendPacketAsync(player.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                return;
            }

            await player.SendPacketAsync(added.GeneratePocketChange());
            await player.SendPacketAsync(player.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                2, vNum.ToString(), amount));
        }

        private async Task ReturnAsync(ClientSession session)
        {
            var player = session.Character;
            await player.OnMapAsync(() =>
            {
                player.RainbowBattleTeam = null;
                player.RainbowBattleThawsAt = null;
                player.RainbowBattleKills = 0;
                player.NoMove = false;
                player.NoAttack = false;
                return Task.CompletedTask;
            });
            if (!player.IsAlive)
            {
                await player.ReviveAsync(fully: true);
            }

            await player.SendPacketAsync(player.GenerateCond());
            await player.SendPacketAsync(new FbtWindowPacket { IsInBattle = false });
            await player.SendPacketAsync(new FbtLeavePacket());
            await mapChangeService.ChangeMapAsync(session, player.MapId, player.MapX, player.MapY);
        }

        private static BSInfoPacket Search(BsInfoMode mode, short seconds) => new()
        {
            Mode = mode,
            Title = BscPacketType.RainbowBattle,
            Time = seconds,
            Text = BsInfoText.WaitForEntry,
        };

        private static MsgiPacket Message(Game18NConstString message) => new() { Type = MessageType.Default, Message = message };

        private async Task BroadcastAsync(Game18NConstString message)
        {
            foreach (var type in new[] { MessageType.Default, MessageType.Notification })
            {
                await sessionRegistry.BroadcastPacketAsync(new MsgiPacket { Type = type, Message = message });
            }
        }

        private sealed class Crystal(short vNum, RainbowCrystalSize size)
        {
            public short VNum { get; } = vNum;

            public RainbowCrystalSize Size { get; } = size;

            public RainbowBattleTeamType Holder { get; set; } = RainbowBattleTeamType.None;
        }

        // One battle's teams, crystals and score. Every hook runs on the run's own queue, one at a time.
        private sealed class RunHost(RainbowBattleService service, ScriptedInstanceDefinition definition) : IInstanceRunHost
        {
            private readonly Dictionary<RainbowBattleTeamType, List<long>> _members = new()
            {
                [RainbowBattleTeamType.Red] = [],
                [RainbowBattleTeamType.Blue] = [],
            };

            private readonly Dictionary<RainbowBattleTeamType, int> _points = new()
            {
                [RainbowBattleTeamType.Red] = 0,
                [RainbowBattleTeamType.Blue] = 0,
            };

            private readonly Dictionary<long, Crystal> _crystals = [];
            private Instant? _scoresAt;

            public string Name => definition.Title ?? Script;

            public bool StartsOnFirstEntry => false;

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public void Join(long characterId, RainbowBattleTeamType team) => _members[team].Add(characterId);

            public bool CanCapture(long crystalId, RainbowBattleTeamType team) =>
                _crystals.TryGetValue(crystalId, out var crystal) && crystal.Holder != team;

            public (short X, short Y) BaseCell(MapInstance room, RainbowBattleTeamType team)
            {
                var index = team == RainbowBattleTeamType.Red ? 0 : 1;
                if (index >= definition.Bases.Count)
                {
                    return room.Map.RandomWalkableCell(service.Random, 100) ?? (0, 0);
                }

                var (from, to) = (definition.Bases[index].From, definition.Bases[index].To);
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    var x = (short)service.Random.Next(Math.Min(from.X, to.X), Math.Max(from.X, to.X) + 1);
                    var y = (short)service.Random.Next(Math.Min(from.Y, to.Y), Math.Max(from.Y, to.Y) + 1);
                    if (room.Map.IsWalkable(x, y))
                    {
                        return (x, y);
                    }
                }

                return (from.X, from.Y);
            }

            public async Task WelcomeAsync(PlayerComponentBundle player, RainbowBattleTeamType team)
            {
                await player.SendPacketAsync(new FbtWindowPacket { IsInBattle = true });
                await player.SendPacketAsync(new FbtMembersPacket { CharacterIds = [.. _members[team]] });
                await player.SendPacketAsync(MemberList(team));
                await player.SendPacketAsync(Scores(team));
                await player.SendPacketAsync(Message(team == RainbowBattleTeamType.Red
                    ? Game18NConstString.InRedTeam
                    : Game18NConstString.InBlueTeam));
            }

            public async Task NpcPlacedAsync(ScriptedInstanceRun run, MapInstance room, NpcComponentBundle npc)
            {
                if (SizeOf(npc.VNum) is not { } size)
                {
                    return;
                }

                _crystals[npc.VisualId] = new Crystal(npc.VNum, size);
                await room.SendPacketAsync(new FbtCrystalPacket
                {
                    NpcId = npc.VisualId,
                    Size = size,
                    Team = RainbowBattleTeamType.None,
                });
            }

            public Task CountdownAsync(ScriptedInstanceRun run, MapInstance room, TimeSpan length) =>
                room.SendPacketAsync(new FbtClockPacket { Mode = FbtClockMode.Start, Seconds = (int)length.TotalSeconds });

            public async Task CaptureAsync(ScriptedInstanceRun run, PlayerComponentBundle player, NpcComponentBundle npc)
            {
                if (player.RainbowBattleTeam is not { } team || player.RainbowBattleThawsAt != null
                    || !_crystals.TryGetValue(npc.VisualId, out var crystal) || crystal.Holder == team)
                {
                    return;
                }

                var lostBy = crystal.Holder;
                crystal.Holder = team;
                await run.BroadcastAsync(new FbtCrystalPacket { NpcId = npc.VisualId, Size = crystal.Size, Team = team });
                var opponent = Opponent(team);
                await SendAsync(opponent, new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = lostBy == opponent ? Game18NConstString.FlagCapturedByEnemies : Game18NConstString.EnemyHasCapturedTheFlagX,
                    ArgumentType = 8,
                    Game18NArguments = { crystal.VNum },
                });
                await SendScoresAsync();
            }

            public async Task RescueAsync(ScriptedInstanceRun run, PlayerComponentBundle rescuer, PlayerComponentBundle frozen)
            {
                if (rescuer.RainbowBattleTeam is not { } team || rescuer.RainbowBattleThawsAt != null
                    || frozen.RainbowBattleTeam != team || frozen.RainbowBattleThawsAt == null)
                {
                    return;
                }

                await ThawAsync(run, frozen, toBase: false);
                await run.BroadcastAsync(new Msgi2Packet
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.PlayerFreedPlayerXFromTheIce,
                    ArgumentType = 2,
                    Game18NArguments = { rescuer.Name, frozen.Name },
                });
            }

            public async Task DefeatedAsync(ScriptedInstanceRun run, PlayerComponentBundle winner)
            {
                if (winner.RainbowBattleTeam is not { } team)
                {
                    return;
                }

                winner.RainbowBattleKills++;
                await run.BroadcastAsync(new Msgi2Packet
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.AmoutOfPlayersDefeatedByPlayerX,
                    ArgumentType = 3,
                    Game18NArguments = { winner.Name, winner.RainbowBattleKills },
                });
                await SendAsync(team, MemberList(team));
            }

            public async Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player)
            {
                await player.OnMapAsync(() =>
                {
                    player.Hp = player.MaxHp;
                    player.Mp = player.MaxMp;
                    player.IsAlive = true;
                    player.NoMove = true;
                    player.NoAttack = true;
                    player.RainbowBattleThawsAt = service.Clock.GetCurrentInstant().Plus(Duration.FromTimeSpan(FrozenTime));
                    return Task.CompletedTask;
                });
                if (player.MapInstance is { } room)
                {
                    await room.SendPacketAsync(new RevivePacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = player.VisualId,
                        Data = 0,
                    });
                    await room.SendPacketAsync(player.GenerateEff(IceBreakerService.IceBreakerService.FrozenEffect));
                }

                await player.SendPacketAsync(player.GenerateStat());
                await player.SendPacketAsync(player.GenerateCond());
                await player.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.IceEffect,
                    ArgumentType = 4,
                    Game18NArguments = { (int)FrozenTime.TotalSeconds },
                });
            }

            public async Task TickAsync(ScriptedInstanceRun run)
            {
                var now = service.Clock.GetCurrentInstant();
                foreach (var session in run.Players().Where(s => s.HasPlayerEntity).ToList())
                {
                    var player = session.Character;
                    if (player.RainbowBattleThawsAt is not { } thawsAt)
                    {
                        continue;
                    }

                    if (now >= thawsAt)
                    {
                        await ThawAsync(run, player, toBase: true);
                    }
                    else
                    {
                        await run.BroadcastAsync(player.GenerateEff(IceBreakerService.IceBreakerService.FrozenEffect));
                    }
                }

                _scoresAt ??= now.Plus(Duration.FromTimeSpan(ScoringInterval));
                if (now >= _scoresAt)
                {
                    _scoresAt = _scoresAt.Value.Plus(Duration.FromTimeSpan(ScoringInterval));
                    await ScoreAsync(run);
                }

                foreach (var team in _members.Keys)
                {
                    await SendAsync(team, new FbtHealthPacket
                    {
                        Members = Players(team).Select(p => (FbtHealthSubPacket?)new FbtHealthSubPacket
                        {
                            CharacterId = p.CharacterId,
                            HpPercentage = p.MaxHp == 0 ? 0 : (int)(p.Hp * 100L / p.MaxHp),
                            MpPercentage = p.MaxMp == 0 ? 0 : (int)(p.Mp * 100L / p.MaxMp),
                        }).ToList(),
                    });
                }
            }

            public async Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
            {
                await run.BroadcastAsync(new FbtClockPacket { Mode = FbtClockMode.Hide });
                if (state != EndStateType.Success)
                {
                    return;
                }

                var red = _points[RainbowBattleTeamType.Red];
                var blue = _points[RainbowBattleTeamType.Blue];
                RainbowBattleTeamType? winner = red == blue ? null
                    : red > blue ? RainbowBattleTeamType.Red : RainbowBattleTeamType.Blue;
                await run.BroadcastAsync(Message(winner switch
                {
                    RainbowBattleTeamType.Red => Game18NConstString.RedTeamWon,
                    RainbowBattleTeamType.Blue => Game18NConstString.BlueTeamWon,
                    _ => Game18NConstString.Draw,
                }));
                foreach (var session in run.Players().Where(s => s.HasPlayerEntity).ToList())
                {
                    var player = session.Character;
                    if (player.RainbowBattleTeam is not { } team)
                    {
                        continue;
                    }

                    var won = winner == team;
                    if (winner != null)
                    {
                        await player.SendPacketAsync(Message(won ? Game18NConstString.WonRainbowBattle : Game18NConstString.LostRainbowBattle));
                    }

                    foreach (var gift in won ? definition.GiftItems : definition.ConsolationItems)
                    {
                        await service.RewardAsync(player, gift.VNum, gift.Amount);
                    }
                }
            }

            public Task ReturnAsync(ClientSession session) => service.ReturnAsync(session);

            private async Task ScoreAsync(ScriptedInstanceRun run)
            {
                foreach (var team in _members.Keys)
                {
                    var earned = _crystals.Values.Where(c => c.Holder == team).Sum(c => PointsOf(c.Size));
                    if (earned == 0)
                    {
                        continue;
                    }

                    _points[team] += earned;
                    await run.BroadcastAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = team == RainbowBattleTeamType.Red ? Game18NConstString.RedEarnedPoints : Game18NConstString.BlueEarnedPoints,
                        ArgumentType = 4,
                        Game18NArguments = { earned },
                    });
                }

                await SendScoresAsync();
            }

            private async Task ThawAsync(ScriptedInstanceRun run, PlayerComponentBundle player, bool toBase)
            {
                run.StoodUp(player.CharacterId);
                await player.OnMapAsync(() =>
                {
                    player.RainbowBattleThawsAt = null;
                    player.NoMove = false;
                    player.NoAttack = false;
                    return Task.CompletedTask;
                });
                if (toBase && player.MapInstance is { } room && player.RainbowBattleTeam is { } team)
                {
                    var (x, y) = BaseCell(room, team);
                    await player.TeleportAsync(x, y);
                }

                await player.SendPacketAsync(player.GenerateCond());
            }

            private Task SendScoresAsync() =>
                Task.WhenAll(_members.Keys.Select(team => SendAsync(team, Scores(team))));

            private FbsPacket Scores(RainbowBattleTeamType team)
            {
                var held = _crystals.Values.Where(c => c.Holder == team).ToList();
                return new FbsPacket
                {
                    TeamId = (byte)team,
                    TeamCount = _members[team].Count,
                    RedPoints = _points[RainbowBattleTeamType.Red],
                    BluePoints = _points[RainbowBattleTeamType.Blue],
                    SmallFlags = held.Count(c => c.Size == RainbowCrystalSize.Small),
                    MediumFlags = held.Count(c => c.Size == RainbowCrystalSize.Medium),
                    BigFlags = held.Count(c => c.Size == RainbowCrystalSize.Large),
                };
            }

            private FblstPacket MemberList(RainbowBattleTeamType team) => new()
            {
                Members = Players(team).Select(p => (FblstSubPacket?)new FblstSubPacket
                {
                    Level = p.Level,
                    Morph = p.UseSp ? p.Morph : -1,
                    Class = p.Class,
                    Kills = p.RainbowBattleKills,
                    Name = p.Name,
                    Gender = p.Gender,
                    CharacterId = p.CharacterId,
                    HeroLevel = p.HeroLevel,
                }).ToList(),
            };

            private IEnumerable<PlayerComponentBundle> Players(RainbowBattleTeamType team) =>
                _members[team]
                    .Select(service.Session)
                    .OfType<ClientSession>()
                    .Where(s => s.HasPlayerEntity && s.Character.RainbowBattleTeam == team)
                    .Select(s => s.Character);

            private async Task SendAsync(RainbowBattleTeamType team, NosCore.Packets.Interfaces.IPacket packet)
            {
                foreach (var player in Players(team).ToList())
                {
                    await player.SendPacketAsync(packet);
                }
            }

            private static RainbowBattleTeamType Opponent(RainbowBattleTeamType team) =>
                team == RainbowBattleTeamType.Red ? RainbowBattleTeamType.Blue : RainbowBattleTeamType.Red;
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Arena;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TalentArenaService
{
    // Players register alone in the arena and are put into teams of three of similar level; two
    // teams then fight a match in their own copy of the ring, which runs from its script.
    public class TalentArenaService(
        IScriptedInstanceService scriptedInstanceService,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IItemGenerationService itemGenerationService,
        ILogger<TalentArenaService> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage) : ITalentArenaService, ISingletonService
    {
        public const string Script = "talent-arena";

        public static readonly TimeSpan OpenLength = TimeSpan.FromHours(4);
        public static readonly TimeSpan SearchTime = TimeSpan.FromMinutes(5);
        public const int CallsPerTeam = 5;
        public static readonly TimeSpan CallDelay = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan CallLength = TimeSpan.FromSeconds(30);
        private const int CallEffect = 4432;
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly List<Registrant> _registrants = new();
        private IInstanceScript? _script;
        private int _running;
        private int _nextTeam;

        public bool IsOpen => Volatile.Read(ref _running) == 1;

        public bool TryStart()
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                return false;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await RunAsync();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Arena of Talents failed");
                }
                finally
                {
                    await CloseRegistrationAsync();
                    Volatile.Write(ref _running, 0);
                }
            });
            return true;
        }

        public async Task RegisterAsync(ClientSession session)
        {
            var player = session.Character;
            if (!IsOpen)
            {
                await player.SendPacketAsync(Message(Game18NConstString.NotAuthorised));
                return;
            }

            if (player.MapInstance?.MapInstanceType != MapInstanceType.ArenaInstance)
            {
                await player.SendPacketAsync(Message(Game18NConstString.RegisterAtArena));
                return;
            }

            if (player.Group.Count > 1)
            {
                await player.SendPacketAsync(Message(Game18NConstString.TeamMembersCantUseArena));
                return;
            }

            await _lock.WaitAsync();
            try
            {
                if (player.TalentArenaTeam != null || _registrants.Any(r => r.CharacterId == player.CharacterId))
                {
                    return;
                }

                _registrants.Add(new Registrant(player.CharacterId, player.MapInstanceId, player.PositionX, player.PositionY)
                {
                    Team = Interlocked.Increment(ref _nextTeam),
                    SecondsLeft = (int)SearchTime.TotalSeconds,
                });
                await player.SendPacketAsync(Search(BsInfoMode.Open, (short)SearchTime.TotalSeconds, BsInfoText.SearchTeammates));
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task CancelAsync(ClientSession session, bool confirmed)
        {
            await _lock.WaitAsync();
            try
            {
                var registrant = _registrants.Find(r => r.CharacterId == session.Character.CharacterId);
                if (registrant == null)
                {
                    return;
                }

                var teammates = _registrants.Where(r => r.Team == registrant.Team && r != registrant).ToList();
                if (teammates.Count > 0 && !confirmed)
                {
                    await session.SendPacketAsync(new QnaiPacket
                    {
                        YesPacket = new BscPacket { Type = BscPacketType.TalentArena, Option = 1 },
                        Question = Game18NConstString.IfLeavingTeamPlayerWillReceivePunishment,
                    });
                    return;
                }

                _registrants.Remove(registrant);
                await session.SendPacketAsync(Search(BsInfoMode.Close, 0, BsInfoText.WaitForEntry));
                foreach (var teammate in teammates)
                {
                    teammate.Next = BsInfoText.SearchTeammatesAgain;
                    if (Session(teammate.CharacterId) is { } other)
                    {
                        await other.SendPacketAsync(new Msgi2Packet
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.LeftArenaTeam,
                            ArgumentType = 1,
                            Game18NArguments = { session.Character.Name },
                        });
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public Task CallAsync(ClientSession session, byte calledIndex) =>
            scriptedInstanceService.GetRun(session.Character.MapInstanceId) is { Host: Match match } run
                ? run.RunHostAsync(() => match.CallAsync(run, session.Character, calledIndex))
                : Task.CompletedTask;

        private async Task RunAsync()
        {
            var started = Stopwatch.GetTimestamp();
            for (var step = 1; Tick * step <= OpenLength; step++)
            {
                await _lock.WaitAsync();
                try
                {
                    await MatchmakeAsync();
                }
                finally
                {
                    _lock.Release();
                }

                var wait = Tick * step - Stopwatch.GetElapsedTime(started);
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait);
                }
            }
        }

        private async Task MatchmakeAsync()
        {
            foreach (var gone in _registrants.Where(r => Session(r.CharacterId) is not { } s
                         || s.Character.MapInstance?.MapInstanceType != MapInstanceType.ArenaInstance).ToList())
            {
                _registrants.Remove(gone);
                foreach (var teammate in _registrants.Where(r => r.Team == gone.Team))
                {
                    teammate.Next = BsInfoText.SearchTeammatesAgain;
                }
            }

            foreach (var registrant in _registrants)
            {
                if (registrant.Next is { } next)
                {
                    registrant.Next = null;
                    registrant.SecondsLeft = (int)SearchTime.TotalSeconds;
                    await SendAsync(registrant, Search(BsInfoMode.Update, (short)SearchTime.TotalSeconds, next));
                    continue;
                }

                if (--registrant.SecondsLeft > 0)
                {
                    continue;
                }

                var size = TeamOf(registrant).Count;
                await SendAsync(registrant, Search(BsInfoMode.Update, -1,
                    size == TalentArenaTeams.TeamSize ? BsInfoText.NoOpposingTeamFound : BsInfoText.NoTeammatesFound));
                registrant.Next = size == TalentArenaTeams.TeamSize ? BsInfoText.SearchOpposingTeam
                    : size == 1 ? BsInfoText.SearchTeammates : BsInfoText.SearchTeammatesAgain;
            }

            await FormTeamsAsync();
            await PairTeamsAsync();
        }

        private async Task FormTeamsAsync()
        {
            foreach (var registrant in _registrants.ToList())
            {
                var team = TeamOf(registrant);
                if (team.Count >= TalentArenaTeams.TeamSize)
                {
                    continue;
                }

                var other = _registrants.Where(r => r.Team != registrant.Team).Select(TeamOf)
                    .FirstOrDefault(o => TalentArenaTeams.CanTeamUp(Entrants(team), Entrants(o)));
                if (other == null)
                {
                    continue;
                }

                foreach (var joiner in other)
                {
                    joiner.Team = registrant.Team;
                }

                var merged = TeamOf(registrant);
                foreach (var member in merged)
                {
                    member.Next = merged.Count == TalentArenaTeams.TeamSize ? BsInfoText.SearchOpposingTeam
                        : BsInfoText.SearchTeammatesAgain;
                    await SendAsync(member, Search(BsInfoMode.Update, -1, BsInfoText.TeammatesFound));
                }
            }
        }

        private async Task PairTeamsAsync()
        {
            var teams = _registrants.Where(r => r.Next == null).GroupBy(r => r.Team).Select(g => g.ToList())
                .Where(t => t.Count == TalentArenaTeams.TeamSize).ToList();
            while (teams.Count >= 2)
            {
                var zenas = teams[0];
                var erenia = teams.Skip(1).FirstOrDefault(t => TalentArenaTeams.AreRivals(Entrants(zenas), Entrants(t)));
                teams.RemoveAt(0);
                if (erenia == null)
                {
                    continue;
                }

                teams.Remove(erenia);
                foreach (var registrant in zenas.Concat(erenia))
                {
                    _registrants.Remove(registrant);
                    await SendAsync(registrant, Search(BsInfoMode.Update, -1, BsInfoText.OpposingTeamFound));
                    await SendAsync(registrant, Search(BsInfoMode.Close, 0, BsInfoText.WaitForEntry));
                }

                await CreateMatchAsync(zenas, erenia);
            }
        }

        private async Task CreateMatchAsync(List<Registrant> zenas, List<Registrant> erenia)
        {
            _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
            if (_script is not { Definition.Sides.Count: 2 } script)
            {
                logger.LogError(logLanguage[LogLanguageKey.TALENT_ARENA_SCRIPT_MISSING], Script);
                return;
            }

            var fighters = zenas.Select((r, i) => new Fighter(r, ArenaTeamType.Zenas, (byte)i))
                .Concat(erenia.Select((r, i) => new Fighter(r, ArenaTeamType.Erenia, (byte)i))).ToList();
            var match = new Match(this, script.Definition, fighters);
            var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.TalentArenaMapInstance, match);
            if (run == null)
            {
                return;
            }

            var ring = run.Rooms[script.Definition.Rooms[0].Key];
            foreach (var fighter in fighters)
            {
                if (Session(fighter.CharacterId) is not { } session)
                {
                    fighter.Defeated = true;
                    fighter.Left = true;
                    continue;
                }

                var player = session.Character;
                await player.OnMapAsync(() =>
                {
                    player.Hp = player.MaxHp;
                    player.Mp = player.MaxMp;
                    player.TalentArenaTeam = fighter.Team;
                    player.InTalentArenaRing = false;
                    return Task.CompletedTask;
                });
                var entry = match.Side(fighter.Team).Entry;
                await session.SendPacketAsync(new TeamArenaClosePacket());
                await mapChangeService.ChangeMapInstanceAsync(session, ring, entry.X, (short)(entry.Y + fighter.Order * 2));
                await session.SendPacketAsync(match.Scores(TaWindowMode.Open));
                await session.SendPacketAsync(match.Lists(fighter.Team, TaWindowMode.Open));
                await session.SendPacketAsync(new TaSvPacket { IsHidden = false });
                await session.SendPacketAsync(new TaStPacket { Type = TaStType.Watch });
                await session.SendPacketAsync(new TaPnPacket { Position = (byte)(fighter.Order + 1) });
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.VisualId,
                    Type = SayColorType.Yellow,
                    Message = fighter.Team == ArenaTeamType.Zenas ? Game18NConstString.Zenas : Game18NConstString.Erenia,
                });
            }

            if (!await scriptedInstanceService.DisposeIfEmptyAsync(run))
            {
                await run.StartAsync();
            }
        }

        private async Task RewardAsync(PlayerComponentBundle player, short vnum, short amount)
        {
            var item = itemGenerationService.Create(vnum, amount);
            var added = player.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, player.CharacterId))?.FirstOrDefault();
            if (added == null)
            {
                await player.SendPacketAsync(player.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                return;
            }

            await player.SendPacketAsync(added.GeneratePocketChange(added.Type.ToPocketType(), added.Slot));
            await player.SendPacketAsync(player.GenerateSayi(SayColorType.Green, Game18NConstString.RewardFromArenaTalents,
                2, vnum.ToString(), amount));
        }

        private IMapChangeService MapChange => mapChangeService;

        private IEnumerable<ClientSession> SessionsOn(Guid mapInstanceId) =>
            sessionRegistry.GetClientSessionsByMapInstance(mapInstanceId).Where(s => s.HasPlayerEntity);

        private async Task CloseRegistrationAsync()
        {
            await _lock.WaitAsync();
            try
            {
                foreach (var registrant in _registrants)
                {
                    await SendAsync(registrant, Search(BsInfoMode.Close, 0, BsInfoText.WaitForEntry));
                }

                _registrants.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }

        // A draw counts as neither a win nor a loss.
        public static void Record(CharacterDto record, ArenaTeamType team, ArenaResultType result)
        {
            if (result == ArenaResultType.Draw)
            {
                return;
            }

            if (result == (team == ArenaTeamType.Zenas ? ArenaResultType.ZenasWon : ArenaResultType.EreniaWon))
            {
                record.TalentWin++;
            }
            else
            {
                record.TalentLose++;
            }
        }

        public static ArenaResultType? Result(IReadOnlyCollection<(ArenaTeamType Team, bool Defeated)> fighters)
        {
            var zenasLeft = fighters.Any(f => f.Team == ArenaTeamType.Zenas && !f.Defeated);
            var ereniaLeft = fighters.Any(f => f.Team == ArenaTeamType.Erenia && !f.Defeated);
            return (zenasLeft, ereniaLeft) switch
            {
                (true, true) => null,
                (true, false) => ArenaResultType.ZenasWon,
                (false, true) => ArenaResultType.EreniaWon,
                _ => ArenaResultType.Draw,
            };
        }

        // The teammate a fighter in the ring brings in: the calledIndex-th of their teammates in
        // fighting order, while that one is still in the match, on the bench and not already called.
        public static long? CallTarget(IReadOnlyCollection<TalentArenaFighter> fighters, long callerId, byte calledIndex, int callsUsed)
        {
            var caller = fighters.FirstOrDefault(f => f.CharacterId == callerId);
            if (caller is not { InRing: true, Defeated: false } || callsUsed >= CallsPerTeam)
            {
                return null;
            }

            var called = fighters.Where(f => f.Team == caller.Team && f.CharacterId != callerId).OrderBy(f => f.Order)
                .ElementAtOrDefault(calledIndex);
            return called is { Defeated: false, InRing: false, Called: false } ? called.CharacterId : null;
        }

        private static ArenaTeamType Opponent(ArenaTeamType team) =>
            team == ArenaTeamType.Zenas ? ArenaTeamType.Erenia : ArenaTeamType.Zenas;

        private ClientSession? Session(long characterId) => sessionRegistry.GetSessionByCharacterId(characterId);

        private async Task SendAsync(Registrant registrant, IPacket packet)
        {
            if (Session(registrant.CharacterId) is { } session)
            {
                await session.SendPacketAsync(packet);
            }
        }

        private List<Registrant> TeamOf(Registrant registrant) => _registrants.Where(r => r.Team == registrant.Team).ToList();

        private List<TalentArenaEntrant> Entrants(IEnumerable<Registrant> team) =>
            team.Select(r => new TalentArenaEntrant(r.CharacterId, Session(r.CharacterId)?.Character.Level ?? 0)).ToList();

        private static BSInfoPacket Search(BsInfoMode mode, short seconds, BsInfoText text) => new()
        {
            Mode = mode,
            Title = BscPacketType.TalentArena,
            Time = seconds,
            Text = text,
        };

        private static MsgiPacket Message(Game18NConstString message) => new() { Type = MessageType.Default, Message = message };

        private sealed record Registrant(long CharacterId, Guid ReturnMapInstanceId, short ReturnX, short ReturnY)
        {
            public int Team { get; set; }

            public int SecondsLeft { get; set; }

            public BsInfoText? Next { get; set; }
        }

        private sealed class Fighter(Registrant registrant, ArenaTeamType team, byte order)
        {
            public long CharacterId { get; } = registrant.CharacterId;

            public Guid ReturnMapInstanceId { get; } = registrant.ReturnMapInstanceId;

            public short ReturnX { get; } = registrant.ReturnX;

            public short ReturnY { get; } = registrant.ReturnY;

            public ArenaTeamType Team { get; } = team;

            public byte Order { get; } = order;

            public bool InRing { get; set; }

            public bool Defeated { get; set; }

            public bool Left { get; set; }

            // Set while a teammate's call brings this fighter into the ring.
            public Call? Call { get; set; }
        }

        private sealed class Call(TimeSpan arrivesAt)
        {
            public TimeSpan ArrivesAt { get; } = arrivesAt;

            public TimeSpan? EndsAt { get; set; }
        }

        // One match: who fights for each side, in what order, and the score. The script decides
        // when bouts start and end; this carries them out and draws the arena windows.
        private sealed class Match(TalentArenaService service, ScriptedInstanceDefinition definition, List<Fighter> fighters)
            : IInstanceRunHost
        {
            private readonly Dictionary<ArenaTeamType, int> _wins = new() { [ArenaTeamType.Zenas] = 0, [ArenaTeamType.Erenia] = 0 };
            private readonly Dictionary<ArenaTeamType, int> _calls = new() { [ArenaTeamType.Zenas] = 0, [ArenaTeamType.Erenia] = 0 };

            public string Name => "talent arena";

            public bool StartsOnFirstEntry => false;

            public BoutSide Side(ArenaTeamType team) => definition.Sides[team == ArenaTeamType.Zenas ? 0 : 1];

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public bool CallFighters(ScriptedInstanceRun run, MapInstance room)
            {
                var zenas = Next(ArenaTeamType.Zenas);
                var erenia = Next(ArenaTeamType.Erenia);
                if (zenas == null || erenia == null)
                {
                    return false;
                }

                var called = new[] { zenas, erenia }.Where(f => !f.InRing).ToList();
                foreach (var fighter in called)
                {
                    fighter.InRing = true;
                    fighter.Call = null;
                }

                run.Queue(() => StartBoutAsync(room, called));
                return true;
            }

            public void OpenBout(ScriptedInstanceRun run, MapInstance room)
            {
                foreach (var fighter in fighters.Where(f => f.InRing && !f.Defeated))
                {
                    if (service.Session(fighter.CharacterId) is { } session)
                    {
                        session.Character.InTalentArenaRing = true;
                    }
                }
            }

            public int FightersStanding(ScriptedInstanceRun run, MapInstance room) => fighters.Count(f => f.InRing && !f.Defeated);

            public void EndBout(ScriptedInstanceRun run, MapInstance room)
            {
                foreach (var fighter in fighters.Where(f => f.InRing && !f.Defeated).ToList())
                {
                    Fall(fighter, givesWin: false);
                    run.Queue(() => BenchAsync(run, fighter, null));
                }
            }

            public Task CountdownAsync(ScriptedInstanceRun run, MapInstance room, TimeSpan length) =>
                room.SendPacketAsync(Scores(TaWindowMode.Clock, (int)length.TotalSeconds));

            public async Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player)
            {
                var fighter = fighters.Find(f => f.CharacterId == player.CharacterId);
                if (fighter is { Call: not null })
                {
                    await EndCallAsync(run, fighter);
                    return;
                }

                if (fighter is not { InRing: true, Defeated: false })
                {
                    await StandUpAsync(run, player);
                    return;
                }

                var winner = Fall(fighter, givesWin: true);
                await BenchAsync(run, fighter, winner);
            }

            public async Task TickAsync(ScriptedInstanceRun run)
            {
                foreach (var fighter in fighters.Where(f => !f.Defeated && !f.Left))
                {
                    if (service.Session(fighter.CharacterId) is { } session && run.Rooms.Values.Contains(session.Character.MapInstanceId))
                    {
                        continue;
                    }

                    fighter.Left = true;
                    if (fighter.InRing)
                    {
                        Fall(fighter, givesWin: true);
                    }

                    fighter.Defeated = true;
                }

                foreach (var called in fighters.Where(f => f.Call != null).ToList())
                {
                    if (called.Call is { } call)
                    {
                        await AdvanceCallAsync(run, called, call);
                    }
                }

                if (fighters.Any(f => f.InRing && !f.Defeated))
                {
                    await run.BroadcastAsync(Bars());
                }
            }

            public async Task CallAsync(ScriptedInstanceRun run, PlayerComponentBundle player, byte calledIndex)
            {
                var caller = fighters.Find(f => f.CharacterId == player.CharacterId);
                if (caller == null || !player.InTalentArenaRing)
                {
                    return;
                }

                var target = CallTarget(fighters.Select(f => new TalentArenaFighter(f.CharacterId, f.Team, f.Order, f.InRing,
                    f.Defeated, f.Call != null)).ToList(), caller.CharacterId, calledIndex, _calls[caller.Team]);
                var called = fighters.Find(f => f.CharacterId == target);
                if (called == null || service.Session(called.CharacterId) is not { } session
                    || !run.Rooms.Values.Contains(session.Character.MapInstanceId))
                {
                    return;
                }

                _calls[caller.Team]++;
                called.Call = new Call(run.Elapsed + CallDelay);
                await run.BroadcastAsync(player.GenerateEff(CallEffect));
                await SendListsAsync(run);
                await session.SendPacketAsync(Called((int)CallDelay.TotalSeconds));
            }

            private async Task AdvanceCallAsync(ScriptedInstanceRun run, Fighter called, Call call)
            {
                var caller = fighters.Find(f => f.Team == called.Team && f.InRing && !f.Defeated);
                if (caller == null || service.Session(called.CharacterId) is not { } session
                    || !run.Rooms.Values.Contains(session.Character.MapInstanceId))
                {
                    await EndCallAsync(run, called);
                    return;
                }

                if (call.EndsAt is { } endsAt)
                {
                    if (run.Elapsed >= endsAt)
                    {
                        await EndCallAsync(run, called);
                    }

                    return;
                }

                var secondsLeft = (int)Math.Ceiling((call.ArrivesAt - run.Elapsed).TotalSeconds);
                if (secondsLeft > 0)
                {
                    await session.SendPacketAsync(Called(secondsLeft));
                    return;
                }

                if (service.Session(caller.CharacterId)?.Character is not { } callerPlayer)
                {
                    await EndCallAsync(run, called);
                    return;
                }

                call.EndsAt = run.Elapsed + CallLength;
                var player = session.Character;
                player.InTalentArenaRing = true;
                await player.TeleportAsync(callerPlayer.PositionX, callerPlayer.PositionY);
                await run.BroadcastAsync(new TaTPacket { CharacterId = called.CharacterId, Seconds = (int)CallLength.TotalSeconds });
                await session.SendPacketAsync(new TaStPacket { Type = TaStType.Nothing });
            }

            private async Task EndCallAsync(ScriptedInstanceRun run, Fighter called)
            {
                called.Call = null;
                if (service.Session(called.CharacterId) is not { } session || !run.Rooms.Values.Contains(session.Character.MapInstanceId))
                {
                    return;
                }

                var player = session.Character;
                player.InTalentArenaRing = false;
                if (run.IsDown(player.CharacterId))
                {
                    await StandUpAsync(run, player);
                }

                var bench = Side(called.Team).Bench;
                await player.TeleportAsync(bench.X, bench.Y);
                await session.SendPacketAsync(new TaStPacket { Type = TaStType.Watch });
            }

            private async Task SendListsAsync(ScriptedInstanceRun run)
            {
                foreach (var session in run.Players())
                {
                    await session.SendPacketAsync(Lists(session.Character.TalentArenaTeam ?? ArenaTeamType.Zenas, TaWindowMode.Update));
                }
            }

            private static MsgiPacket Called(int seconds) => new()
            {
                Type = MessageType.Default,
                Message = Game18NConstString.CalledByAnotherParticipant,
                ArgumentType = 4,
                Game18NArguments = { seconds },
            };

            public async Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
            {
                var result = Result(fighters.Select(f => (f.Team, f.Defeated)).ToList()) ?? ArenaResultType.Draw;
                foreach (var fighter in fighters.Where(f => !f.Left))
                {
                    if (service.Session(fighter.CharacterId) is not { } session || !run.Rooms.Values.Contains(session.Character.MapInstanceId))
                    {
                        continue;
                    }

                    session.Character.InTalentArenaRing = false;
                    await session.SendPacketAsync(Scores(TaWindowMode.Update));
                    await session.SendPacketAsync(Lists(fighter.Team, TaWindowMode.Update));
                    await session.SendPacketAsync(new TaFPacket
                    {
                        Result = result,
                        TeamType = fighter.Team,
                        Won = _wins[fighter.Team],
                        LivesLeft = Lives(fighter.Team),
                        OpponentWon = _wins[Opponent(fighter.Team)],
                        OpponentLivesLeft = Lives(Opponent(fighter.Team)),
                    });
                    var won = result == (fighter.Team == ArenaTeamType.Zenas ? ArenaResultType.ZenasWon : ArenaResultType.EreniaWon);
                    Record(session.Character.CharacterDto, fighter.Team, result);
                    foreach (var gift in won ? definition.GiftItems : definition.ConsolationItems)
                    {
                        await service.RewardAsync(session.Character, gift.VNum, gift.Amount);
                    }
                }

                foreach (var fighter in fighters.Where(f => f.Left))
                {
                    if (service.Session(fighter.CharacterId) is { } session)
                    {
                        session.Character.CharacterDto.TalentSurrender++;
                        session.Character.TalentArenaTeam = null;
                        session.Character.InTalentArenaRing = false;
                    }
                }
            }

            public async Task ReturnAsync(ClientSession session)
            {
                var player = session.Character;
                player.TalentArenaTeam = null;
                player.InTalentArenaRing = false;
                if (!player.IsAlive)
                {
                    await player.ReviveAsync(fully: true);
                }

                if (fighters.Find(f => f.CharacterId == player.CharacterId) is not { } fighter)
                {
                    await service.MapChange.ChangeMapAsync(session, player.MapId, player.MapX, player.MapY);
                    return;
                }

                await session.SendPacketAsync(Scores(TaWindowMode.Close));
                await session.SendPacketAsync(Lists(fighter.Team, TaWindowMode.Close));
                await session.SendPacketAsync(new TaSvPacket { IsHidden = true });
                await session.SendPacketAsync(new TeamArenaClosePacket());
                await service.MapChange.ChangeMapInstanceAsync(session, fighter.ReturnMapInstanceId, fighter.ReturnX, fighter.ReturnY);
            }

            // A fighter knocked out or gone gives the bout to the other side; one still standing when
            // time runs out gives it to nobody.
            private Fighter? Fall(Fighter fighter, bool givesWin)
            {
                var winner = givesWin ? fighters.Find(f => f.InRing && !f.Defeated && f.Team != fighter.Team) : null;
                fighter.Defeated = true;
                fighter.InRing = false;
                if (winner != null)
                {
                    _wins[winner.Team]++;
                }

                return winner;
            }

            private async Task StartBoutAsync(MapInstance room, List<Fighter> called)
            {
                foreach (var fighter in called)
                {
                    if (service.Session(fighter.CharacterId) is { } session)
                    {
                        var ring = Side(fighter.Team).Ring;
                        await session.Character.TeleportAsync(ring.X, ring.Y);
                    }
                }

                foreach (var session in service.SessionsOn(room.MapInstanceId))
                {
                    var team = session.Character.TalentArenaTeam ?? ArenaTeamType.Zenas;
                    var own = Next(team);
                    var opponent = Next(Opponent(team));
                    await session.SendPacketAsync(Lists(team, TaWindowMode.Update));
                    await session.SendPacketAsync(new TaFcPacket { Side = ArenaSideType.Own, CharacterId = own?.CharacterId ?? 0 });
                    await session.SendPacketAsync(new TaFcPacket { Side = ArenaSideType.Opponent, CharacterId = opponent?.CharacterId ?? 0 });
                    await session.SendPacketAsync(new TaStPacket
                    {
                        Type = own?.CharacterId == session.Character.CharacterId ? TaStType.Call : TaStType.Watch,
                    });
                    await session.SendPacketAsync(Scores(TaWindowMode.Update));
                    await session.SendPacketAsync(new TaSPacket());
                }
            }

            private async Task BenchAsync(ScriptedInstanceRun run, Fighter fighter, Fighter? winner)
            {
                if (service.Session(fighter.CharacterId) is not { } session || !run.Rooms.Values.Contains(session.Character.MapInstanceId))
                {
                    return;
                }

                var player = session.Character;
                await StandUpAsync(run, player);
                var bench = Side(fighter.Team).Bench;
                await player.TeleportAsync(bench.X, bench.Y);
                if (winner != null && service.Session(winner.CharacterId) is { } winnerSession)
                {
                    await run.BroadcastAsync(new Msgi2Packet
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.PlayerHasDefeteadPlayerX,
                        ArgumentType = 2,
                        Game18NArguments = { winnerSession.Character.Name, player.Name },
                    });
                }
            }

            private static async Task StandUpAsync(ScriptedInstanceRun run, PlayerComponentBundle player)
            {
                player.InTalentArenaRing = false;
                await player.ReviveAsync(fully: true);
                run.StoodUp(player.CharacterId);
            }

            private Fighter? Next(ArenaTeamType team) =>
                fighters.Where(f => f.Team == team && !f.Defeated).OrderBy(f => f.Order).FirstOrDefault();

            private int Lives(ArenaTeamType team) => fighters.Count(f => f.Team == team && !f.Defeated);

            public TaMPacket Scores(TaWindowMode mode, int seconds = 0) => new()
            {
                Type = mode,
                ZenasScore = _wins[ArenaTeamType.Zenas],
                EreniaScore = _wins[ArenaTeamType.Erenia],
                TimeRemaining = seconds,
            };

            public TaPPacket Lists(ArenaTeamType team, TaWindowMode mode) => new()
            {
                Type = mode,
                TeamType = team,
                CallsLeft = CallsPerTeam - _calls[team],
                OpponentCallsLeft = CallsPerTeam - _calls[Opponent(team)],
                Fighters = Slots(team).Select(f => f == null || service.Session(f.CharacterId)?.Character is not { } p
                        ? new TaPSubPacket()
                        : new TaPSubPacket
                        {
                            IsAlive = !f.Defeated,
                            CharacterId = f.CharacterId,
                            Class = p.Class,
                            Gender = p.Gender,
                            Morph = 0,
                        })
                    .ToList<TaPSubPacket?>(),
            };

            private TaPsPacket Bars() => new()
            {
                Fighters = Slots(ArenaTeamType.Zenas).Select(f => f == null || service.Session(f.CharacterId)?.Character is not { } p
                        ? new TaPsSubPacket()
                        : new TaPsSubPacket
                        {
                            CharacterId = f.CharacterId,
                            HpPercent = p.MaxHp == 0 ? 0 : (int)(p.Hp * 100L / p.MaxHp),
                            MpPercent = p.MaxMp == 0 ? 0 : (int)(p.Mp * 100L / p.MaxMp),
                        })
                    .ToList<TaPsSubPacket?>(),
            };

            // Own team first, each in fighting order, three slots per team.
            private IEnumerable<Fighter?> Slots(ArenaTeamType team) =>
                new[] { team, Opponent(team) }.SelectMany(t => Enumerable.Range(0, TalentArenaTeams.TeamSize)
                    .Select(order => fighters.Find(f => f.Team == t && f.Order == order)));
        }
    }
}

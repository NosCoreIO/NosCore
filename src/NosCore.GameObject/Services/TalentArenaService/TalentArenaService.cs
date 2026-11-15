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
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Arena;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TalentArenaService
{
    // In the Arena of Talents players register alone and are put into teams of three of similar
    // level; in the Arena of Masters a party of three registers together. Two teams of the same
    // arena then fight a match in their own copy of the ring, which runs from its script.
    public class TalentArenaService(
        IScriptedInstanceService scriptedInstanceService,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IItemGenerationService itemGenerationService,
        IRandomProvider random,
        ILogger<TalentArenaService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, TimeProvider time) : ITalentArenaService, ISingletonService
    {
        public const string Script = "talent-arena";

        public static readonly TimeSpan OpenLength = TimeSpan.FromHours(4);
        public static readonly TimeSpan MastersOpenLength = TimeSpan.FromHours(7);
        public const int MasterPointsPerWin = 1;
        public static readonly TimeSpan SearchTime = TimeSpan.FromMinutes(5);
        public const int CallsPerTeam = 5;
        public static readonly TimeSpan CallDelay = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan CallLength = TimeSpan.FromSeconds(30);
        private const int CallEffect = 4432;
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly List<Registrant> _registrants = new();
        private readonly ConcurrentDictionary<ScriptedInstanceRun, byte> _matches = new();
        private readonly ConcurrentDictionary<ArenaKind, long> _openedAt = new();
        private IInstanceScript? _script;
        private int _running;
        private int _nextTeam;

        public bool IsOpen => _openedAt.ContainsKey(ArenaKind.Talents);

        public bool IsMastersOpen => _openedAt.ContainsKey(ArenaKind.Masters);

        public bool TryStart() => TryOpen(ArenaKind.Talents);

        public bool TryStartMasters() => TryOpen(ArenaKind.Masters);

        private bool TryOpen(ArenaKind kind)
        {
            if (!_openedAt.TryAdd(kind, time.GetTimestamp()))
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _running, 1, 0) == 0)
            {
                _ = Task.Run(RunAsync);
            }

            return true;
        }

        private static TimeSpan OpenLengthOf(ArenaKind kind) => kind == ArenaKind.Masters ? MastersOpenLength : OpenLength;

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
                await player.SendPacketAsync(Search(ArenaKind.Talents, BsInfoMode.Open, (short)SearchTime.TotalSeconds, BsInfoText.SearchTeammates));
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RegisterMastersAsync(ClientSession session)
        {
            var player = session.Character;
            if (!IsMastersOpen)
            {
                await player.SendPacketAsync(Message(Game18NConstString.ArenaOfMasterOver));
                return;
            }

            if (player.MapInstance?.MapInstanceType != MapInstanceType.ArenaInstance)
            {
                await player.SendPacketAsync(Message(Game18NConstString.RegisterAtArena));
                return;
            }

            if (player.Group.Count != TalentArenaTeams.TeamSize)
            {
                await player.SendPacketAsync(Message(Game18NConstString.ThreeMemberInGroupRequired));
                return;
            }

            var members = player.Group.Keys.Where(k => k.Item1 == VisualType.Player)
                .Select(k => Session(k.Item2)).ToList();
            if (members.Any(m => m == null || m.Character.MapInstanceId != player.MapInstanceId))
            {
                await player.SendPacketAsync(Message(Game18NConstString.AllParticipantMustStayOnMap));
                return;
            }

            var party = members.OfType<ClientSession>().ToList();
            if (party.Any(m => m.Character.CharacterDto.MasterTicket < 1))
            {
                await player.SendPacketAsync(Message(Game18NConstString.OneParticipantsNumberofEntriesIsTooLowForTheArenaofMasters));
                return;
            }

            await _lock.WaitAsync();
            try
            {
                if (party.Any(m => m.Character.TalentArenaTeam != null || _registrants.Any(r => r.CharacterId == m.Character.CharacterId)))
                {
                    await player.SendPacketAsync(Message(Game18NConstString.AmongTheUsersThereArePlayersWhoAreAlreadyRegistered));
                    return;
                }

                var team = Interlocked.Increment(ref _nextTeam);
                foreach (var member in party)
                {
                    var character = member.Character;
                    _registrants.Add(new Registrant(character.CharacterId, character.MapInstanceId, character.PositionX, character.PositionY)
                    {
                        Kind = ArenaKind.Masters,
                        Team = team,
                        SecondsLeft = (int)SearchTime.TotalSeconds,
                    });
                    await member.SendPacketAsync(Search(ArenaKind.Masters, BsInfoMode.Open, (short)SearchTime.TotalSeconds,
                        BsInfoText.SearchOpposingTeam));
                }
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

                if (registrant.Kind == ArenaKind.Masters)
                {
                    await WithdrawPartyAsync(registrant);
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
                await session.SendPacketAsync(Search(ArenaKind.Talents, BsInfoMode.Close, 0, BsInfoText.WaitForEntry));
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

        public async Task WatchAsync(ClientSession session, string? fighterName)
        {
            var player = session.Character;
            if (player.MapInstance?.MapInstanceType != MapInstanceType.ArenaInstance)
            {
                return;
            }

            await _lock.WaitAsync();
            try
            {
                if (_registrants.Any(r => r.CharacterId == player.CharacterId))
                {
                    await player.SendPacketAsync(Message(Game18NConstString.CanNotWatchWhileRegistered));
                    return;
                }
            }
            finally
            {
                _lock.Release();
            }

            var running = _matches.Keys.Where(r => r.EndState == null).ToList();
            var run = fighterName == null
                ? running.Count == 0 ? null : running[random.Next(0, running.Count)]
                : running.FirstOrDefault(r => ((Match)r.Host).Fights(fighterName));
            if (run == null)
            {
                await player.SendPacketAsync(Message(fighterName == null ? Game18NConstString.NoBattleCurrently
                    : Game18NConstString.OpponentNotInBattle));
                return;
            }

            await run.RunHostAsync(() => ((Match)run.Host).WatchAsync(run, session));
        }

        public Task CallAsync(ClientSession session, byte calledIndex) =>
            scriptedInstanceService.GetRun(session.Character.MapInstanceId) is { Host: Match match } run
                ? run.RunHostAsync(() => match.CallAsync(run, session.Character, calledIndex))
                : Task.CompletedTask;

        // Runs while either arena is open; each closes on its own once its opening time is over.
        private async Task RunAsync()
        {
            try
            {
                _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
                if (_script == null)
                {
                    _openedAt.Clear();
                }

                while (!_openedAt.IsEmpty)
                {
                    await _lock.WaitAsync();
                    try
                    {
                        foreach (var (kind, openedAt) in _openedAt.ToList())
                        {
                            if (time.GetElapsedTime(openedAt) >= OpenLengthOf(kind))
                            {
                                await CloseRegistrationAsync(kind);
                                _openedAt.TryRemove(kind, out _);
                            }
                        }

                        await MatchmakeAsync();
                    }
                    finally
                    {
                        _lock.Release();
                    }

                    await Task.Delay(Tick, time);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, logLanguage[LogLanguageKey.TALENT_ARENA_FAILED]);
                await _lock.WaitAsync();
                try
                {
                    foreach (var kind in _openedAt.Keys.ToList())
                    {
                        await CloseRegistrationAsync(kind);
                        _openedAt.TryRemove(kind, out _);
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
            finally
            {
                Volatile.Write(ref _running, 0);
                if (!_openedAt.IsEmpty && Interlocked.CompareExchange(ref _running, 1, 0) == 0)
                {
                    _ = Task.Run(RunAsync);
                }
            }
        }

        private async Task MatchmakeAsync()
        {
            foreach (var gone in _registrants.Where(r => Session(r.CharacterId) is not { } s
                         || s.Character.MapInstance?.MapInstanceType != MapInstanceType.ArenaInstance).ToList())
            {
                if (!_registrants.Contains(gone))
                {
                    continue;
                }

                if (gone.Kind == ArenaKind.Masters)
                {
                    await WithdrawPartyAsync(gone);
                    continue;
                }

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
                    await SendAsync(registrant, Search(registrant.Kind, BsInfoMode.Update, (short)SearchTime.TotalSeconds, next));
                    continue;
                }

                if (--registrant.SecondsLeft > 0)
                {
                    continue;
                }

                var size = TeamOf(registrant).Count;
                await SendAsync(registrant, Search(registrant.Kind, BsInfoMode.Update, -1,
                    size == TalentArenaTeams.TeamSize ? BsInfoText.NoOpposingTeamFound : BsInfoText.NoTeammatesFound));
                registrant.Next = size == TalentArenaTeams.TeamSize ? BsInfoText.SearchOpposingTeam
                    : size == 1 ? BsInfoText.SearchTeammates : BsInfoText.SearchTeammatesAgain;
            }

            await FormTeamsAsync();
            await PairTeamsAsync();
        }

        private async Task FormTeamsAsync()
        {
            foreach (var registrant in _registrants.Where(r => r.Kind == ArenaKind.Talents).ToList())
            {
                var team = TeamOf(registrant);
                if (team.Count >= TalentArenaTeams.TeamSize)
                {
                    continue;
                }

                var other = _registrants.Where(r => r.Kind == ArenaKind.Talents && r.Team != registrant.Team).Select(TeamOf)
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
                    await SendAsync(member, Search(ArenaKind.Talents, BsInfoMode.Update, -1, BsInfoText.TeammatesFound));
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
                var erenia = teams.Skip(1).FirstOrDefault(t => t[0].Kind == zenas[0].Kind
                    && TalentArenaTeams.AreRivals(Entrants(zenas), Entrants(t)));
                teams.RemoveAt(0);
                if (erenia == null)
                {
                    continue;
                }

                teams.Remove(erenia);
                foreach (var registrant in zenas.Concat(erenia))
                {
                    _registrants.Remove(registrant);
                    await SendAsync(registrant, Search(registrant.Kind, BsInfoMode.Update, -1, BsInfoText.OpposingTeamFound));
                    await SendAsync(registrant, Search(registrant.Kind, BsInfoMode.Close, 0, BsInfoText.WaitForEntry));
                }

                await CreateMatchAsync(zenas[0].Kind, zenas, erenia);
            }
        }

        private async Task CreateMatchAsync(ArenaKind kind, List<Registrant> zenas, List<Registrant> erenia)
        {
            _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
            if (_script is not { Definition.Sides.Count: 2 } script)
            {
                return;
            }

            var fighters = zenas.Select((r, i) => new Fighter(r, ArenaTeamType.Zenas, (byte)i))
                .Concat(erenia.Select((r, i) => new Fighter(r, ArenaTeamType.Erenia, (byte)i))).ToList();
            var match = new Match(this, script.Definition, kind, fighters);
            var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.TalentArenaMapInstance, match);
            if (run == null)
            {
                return;
            }

            _matches[run] = 0;

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
                if (kind == ArenaKind.Masters)
                {
                    player.CharacterDto.MasterTicket = Math.Max(0, player.CharacterDto.MasterTicket - 1);
                }

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

        private async Task CloseRegistrationAsync(ArenaKind kind)
        {
            foreach (var registrant in _registrants.Where(r => r.Kind == kind).ToList())
            {
                _registrants.Remove(registrant);
                await SendAsync(registrant, Search(kind, BsInfoMode.Close, 0, BsInfoText.WaitForEntry));
            }
        }

        // A party registers and leaves the Arena of Masters together.
        private async Task WithdrawPartyAsync(Registrant leaving)
        {
            foreach (var member in TeamOf(leaving))
            {
                _registrants.Remove(member);
                await SendAsync(member, Search(ArenaKind.Masters, BsInfoMode.Close, 0, BsInfoText.WaitForEntry));
                if (member != leaving)
                {
                    await SendAsync(member, Message(Game18NConstString.ParticipantCancelledRegistration));
                }
            }
        }

        // Only a win counts in the Arena of Masters, for MasterPointsPerWin points.
        public static int RecordMasters(CharacterDto record, ArenaTeamType team, ArenaResultType result)
        {
            if (result != (team == ArenaTeamType.Zenas ? ArenaResultType.ZenasWon : ArenaResultType.EreniaWon))
            {
                return 0;
            }

            record.MasterPoints += MasterPointsPerWin;
            return MasterPointsPerWin;
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

        private static BSInfoPacket Search(ArenaKind kind, BsInfoMode mode, short seconds, BsInfoText text) => new()
        {
            Mode = mode,
            Title = kind == ArenaKind.Masters ? BscPacketType.ArenaOfMasters : BscPacketType.TalentArena,
            Time = seconds,
            Text = text,
        };

        private static MsgiPacket Message(Game18NConstString message) => new() { Type = MessageType.Default, Message = message };

        private sealed record Registrant(long CharacterId, Guid ReturnMapInstanceId, short ReturnX, short ReturnY)
        {
            public ArenaKind Kind { get; init; }

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
        private sealed class Match(TalentArenaService service, ScriptedInstanceDefinition definition, ArenaKind kind, List<Fighter> fighters)
            : IInstanceRunHost
        {
            private readonly Dictionary<ArenaTeamType, int> _wins = new() { [ArenaTeamType.Zenas] = 0, [ArenaTeamType.Erenia] = 0 };
            private readonly Dictionary<ArenaTeamType, int> _calls = new() { [ArenaTeamType.Zenas] = 0, [ArenaTeamType.Erenia] = 0 };
            private readonly Dictionary<long, (Guid MapInstanceId, short X, short Y)> _spectators = new();
            private TimeSpan _roundEndsAt;

            public string Name => "talent arena";

            public bool StartsOnFirstEntry => false;

            public bool Fights(string name) => fighters.Any(f => !f.Left
                && string.Equals(service.Session(f.CharacterId)?.Character.Name, name, StringComparison.OrdinalIgnoreCase));

            // Spectators watch from the stands, seeing the match from Zenas' side.
            public async Task WatchAsync(ScriptedInstanceRun run, ClientSession session)
            {
                var player = session.Character;
                _spectators[player.CharacterId] = (player.MapInstanceId, player.PositionX, player.PositionY);
                var ring = run.Rooms[definition.Rooms[0].Key];
                await service.MapChange.ChangeMapInstanceAsync(session, ring, definition.StartX, definition.StartY);
                await session.SendPacketAsync(Scores(TaWindowMode.Open));
                var secondsLeft = (int)Math.Ceiling((_roundEndsAt - run.Elapsed).TotalSeconds);
                if (secondsLeft > 0)
                {
                    await session.SendPacketAsync(Scores(TaWindowMode.Clock, secondsLeft));
                }

                await session.SendPacketAsync(Lists(ArenaTeamType.Zenas, TaWindowMode.Open));
                await session.SendPacketAsync(new TawSvPacket { IsHidden = false });
                await session.SendPacketAsync(new TaFcPacket { Side = ArenaSideType.Own, CharacterId = Fighting(ArenaTeamType.Zenas) });
                await session.SendPacketAsync(new TaFcPacket { Side = ArenaSideType.Opponent, CharacterId = Fighting(ArenaTeamType.Erenia) });
            }

            private long Fighting(ArenaTeamType team) =>
                fighters.Find(f => f.Team == team && f.InRing && !f.Defeated)?.CharacterId ?? 0;

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

            public Task CountdownAsync(ScriptedInstanceRun run, MapInstance room, TimeSpan length)
            {
                _roundEndsAt = run.Elapsed + length;
                return room.SendPacketAsync(Scores(TaWindowMode.Clock, (int)length.TotalSeconds));
            }

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
                service._matches.TryRemove(run, out _);
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
                    if (kind == ArenaKind.Masters)
                    {
                        var points = RecordMasters(session.Character.CharacterDto, fighter.Team, result);
                        if (points > 0)
                        {
                            await session.SendPacketAsync(new MsgiPacket
                            {
                                Type = MessageType.Default,
                                Message = Game18NConstString.PointsEarned,
                                ArgumentType = 4,
                                Game18NArguments = { points },
                            });
                        }

                        continue;
                    }

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
                        if (kind == ArenaKind.Talents)
                        {
                            session.Character.CharacterDto.TalentSurrender++;
                        }

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

                if (_spectators.Remove(player.CharacterId, out var stand))
                {
                    await session.SendPacketAsync(Scores(TaWindowMode.Close));
                    await session.SendPacketAsync(Lists(ArenaTeamType.Zenas, TaWindowMode.Close));
                    await session.SendPacketAsync(new TawSvPacket { IsHidden = true });
                    await session.SendPacketAsync(new TeamArenaClosePacket());
                    await service.MapChange.ChangeMapInstanceAsync(session, stand.MapInstanceId, stand.X, stand.Y);
                    return;
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
                    if (!_spectators.ContainsKey(session.Character.CharacterId))
                    {
                        await session.SendPacketAsync(new TaStPacket
                        {
                            Type = own?.CharacterId == session.Character.CharacterId ? TaStType.Call : TaStType.Watch,
                        });
                    }
                    await session.SendPacketAsync(Scores(TaWindowMode.Update));
                    await session.SendPacketAsync(new TaSPacket());
                }
            }

            private async Task BenchAsync(ScriptedInstanceRun run, Fighter fighter, Fighter? winner)
            {
                foreach (var spectator in run.Players().Where(s => _spectators.ContainsKey(s.Character.CharacterId)))
                {
                    await spectator.SendPacketAsync(new TawDPacket { CharacterId = fighter.CharacterId });
                }

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
                IsArenaOfMasters = kind == ArenaKind.Masters,
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

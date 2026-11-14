//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
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
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Arena;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TalentArenaService
{
    // Players register alone in the arena, are put into teams of three of similar level, and two teams
    // are sent to their own copy of the ring. Each team's fighters come in one at a time in their order;
    // the match ends when a team has nobody left.
    public class TalentArenaService(
        Lazy<IMapInstanceGeneratorService> mapInstanceGeneratorService,
        IMapInstanceRegistry mapInstanceRegistry,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IItemGenerationService itemGenerationService,
        ILogger<TalentArenaService> logger) : ITalentArenaService, ISingletonService
    {
        public const short MapId = 2015;
        public const short WinnerRewardVNum = 2800;
        public const short WinnerRewardAmount = 1;
        public const short LoserRewardVNum = 2801;
        public const short LoserRewardAmount = 3;

        public static readonly TimeSpan OpenLength = TimeSpan.FromHours(4);
        public static readonly TimeSpan SearchTime = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Preparation = TimeSpan.FromSeconds(15);
        public static readonly TimeSpan RoundLength = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan RoundCountdown = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan BetweenRounds = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan ResultTime = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

        private static readonly (short X, short Y) ZenasRing = (56, 40);
        private static readonly (short X, short Y) EreniaRing = (87, 39);
        private static readonly (short X, short Y) ZenasBench = (19, 40);
        private static readonly (short X, short Y) EreniaBench = (120, 39);

        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly List<Registrant> _registrants = new();
        private readonly List<Match> _matches = new();
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
                if (_registrants.Any(r => r.CharacterId == player.CharacterId)
                    || _matches.Any(m => m.Fighters.Any(f => f.CharacterId == player.CharacterId)))
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

        public async Task DefeatAsync(PlayerComponentBundle player)
        {
            await _lock.WaitAsync();
            try
            {
                var match = _matches.Find(m => m.Fighters.Any(f => f.CharacterId == player.CharacterId));
                var fighter = match?.Fighters.Find(f => f.CharacterId == player.CharacterId);
                if (match == null || fighter == null)
                {
                    return;
                }

                if (!fighter.InRing || fighter.Defeated)
                {
                    await ReviveAsync(match, player);
                    return;
                }

                await KnockOutAsync(match, fighter);
            }
            finally
            {
                _lock.Release();
            }
        }

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

                var match = await CreateMatchAsync(zenas, erenia);
                if (match != null)
                {
                    _matches.Add(match);
                    _ = Task.Run(() => RunMatchAsync(match));
                }
            }
        }

        private async Task<Match?> CreateMatchAsync(List<Registrant> zenas, List<Registrant> erenia)
        {
            var baseMap = mapInstanceRegistry.GetBaseMapById(MapId);
            if (baseMap == null)
            {
                logger.LogError("The Arena of Talents needs map {MapId} imported by the parser", MapId);
                return null;
            }

            var mapInstance = mapInstanceGeneratorService.Value.CreateMapInstance(baseMap.Map, Guid.NewGuid(), false,
                MapInstanceType.TalentArenaMapInstance);
            await mapInstanceGeneratorService.Value.AddMapInstanceAsync(mapInstance);
            await mapInstance.StartLifeAsync();

            var fighters = zenas.Select((r, i) => new Fighter(r, ArenaTeamType.Zenas, (byte)i))
                .Concat(erenia.Select((r, i) => new Fighter(r, ArenaTeamType.Erenia, (byte)i))).ToList();
            var match = new Match(mapInstance, fighters);
            foreach (var fighter in fighters)
            {
                if (Session(fighter.CharacterId) is not { } session)
                {
                    fighter.Defeated = true;
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
                var (x, y) = fighter.Team == ArenaTeamType.Zenas
                    ? ((short)14, (short)(38 + fighter.Order * 2))
                    : ((short)125, (short)(37 + fighter.Order * 2));
                await session.SendPacketAsync(new TeamArenaClosePacket());
                await mapChangeService.ChangeMapInstanceAsync(session, mapInstance.MapInstanceId, x, y);
                await session.SendPacketAsync(Scores(match, TaWindowMode.Open));
                await session.SendPacketAsync(Scores(match, TaWindowMode.Clock, (int)Preparation.TotalSeconds));
                await session.SendPacketAsync(Lists(match, fighter.Team, TaWindowMode.Open));
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

            return match;
        }

        private async Task RunMatchAsync(Match match)
        {
            try
            {
                await Task.Delay(Preparation);
                while (true)
                {
                    await _lock.WaitAsync();
                    try
                    {
                        if (Result(match) != null)
                        {
                            break;
                        }

                        await StartRoundAsync(match);
                    }
                    finally
                    {
                        _lock.Release();
                    }

                    var started = Stopwatch.GetTimestamp();
                    var fighting = false;
                    while (true)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                        var elapsed = Stopwatch.GetElapsedTime(started);
                        await _lock.WaitAsync();
                        try
                        {
                            await DropAbsentAsync(match);
                            if (!fighting && elapsed >= RoundCountdown && !match.RoundOver)
                            {
                                fighting = true;
                                await OpenFightAsync(match);
                            }

                            if (!match.RoundOver && elapsed >= RoundCountdown + RoundLength)
                            {
                                // Nobody wins a round that runs out: both fighters are out.
                                foreach (var fighter in match.Fighters.Where(f => f.InRing && !f.Defeated).ToList())
                                {
                                    await KnockOutAsync(match, fighter, false);
                                }
                            }

                            if (match.RoundOver)
                            {
                                break;
                            }

                            await match.MapInstance.SendPacketAsync(Bars(match));
                        }
                        finally
                        {
                            _lock.Release();
                        }
                    }

                    await Task.Delay(BetweenRounds);
                }

                await EndAsync(match);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Arena of Talents match failed");
            }
            finally
            {
                await _lock.WaitAsync();
                _matches.Remove(match);
                _lock.Release();
                await mapInstanceGeneratorService.Value.RemoveMapAsync(match.MapInstance.MapInstanceId);
            }
        }

        private async Task StartRoundAsync(Match match)
        {
            match.RoundOver = false;
            foreach (var team in new[] { ArenaTeamType.Zenas, ArenaTeamType.Erenia })
            {
                var next = Next(match, team);
                if (next == null || next.InRing)
                {
                    continue;
                }

                next.InRing = true;
                if (Session(next.CharacterId) is { } session)
                {
                    var (x, y) = team == ArenaTeamType.Zenas ? ZenasRing : EreniaRing;
                    await session.Character.TeleportAsync(x, y);
                }
            }

            foreach (var session in Spectators(match))
            {
                var team = session.Character.TalentArenaTeam ?? ArenaTeamType.Zenas;
                var own = Next(match, team);
                var opponent = Next(match, Opponent(team));
                await session.SendPacketAsync(Lists(match, team, TaWindowMode.Update));
                await session.SendPacketAsync(new TaFcPacket { Side = ArenaSideType.Own, CharacterId = own?.CharacterId ?? 0 });
                await session.SendPacketAsync(new TaFcPacket { Side = ArenaSideType.Opponent, CharacterId = opponent?.CharacterId ?? 0 });
                await session.SendPacketAsync(new TaStPacket
                {
                    Type = own?.CharacterId == session.Character.CharacterId ? TaStType.Nothing : TaStType.Watch,
                });
                await session.SendPacketAsync(Scores(match, TaWindowMode.Update));
                await session.SendPacketAsync(Scores(match, TaWindowMode.Clock, (int)(RoundCountdown + RoundLength).TotalSeconds));
                await session.SendPacketAsync(new TaSPacket());
            }
        }

        private async Task OpenFightAsync(Match match)
        {
            foreach (var fighter in match.Fighters.Where(f => f.InRing && !f.Defeated))
            {
                if (Session(fighter.CharacterId) is { } session)
                {
                    session.Character.InTalentArenaRing = true;
                }
            }

            await Task.CompletedTask;
        }

        // A fighter knocked out, timed out or gone gives the round to the other team.
        private async Task KnockOutAsync(Match match, Fighter fighter, bool givesWin = true)
        {
            var winner = givesWin ? match.Fighters.Find(f => f.InRing && !f.Defeated && f.Team != fighter.Team) : null;
            fighter.Defeated = true;
            fighter.InRing = false;
            match.RoundOver = true;
            if (winner != null)
            {
                match.Wins[winner.Team]++;
            }

            if (Session(fighter.CharacterId) is not { } session || session.Character.MapInstanceId != match.MapInstance.MapInstanceId)
            {
                return;
            }

            var player = session.Character;
            await ReviveAsync(match, player);
            var (x, y) = fighter.Team == ArenaTeamType.Zenas ? ZenasBench : EreniaBench;
            await player.TeleportAsync(x, y);
            if (winner != null && Session(winner.CharacterId) is { } winnerSession)
            {
                await match.MapInstance.SendPacketAsync(new Msgi2Packet
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.PlayerHasDefeteadPlayerX,
                    ArgumentType = 2,
                    Game18NArguments = { winnerSession.Character.Name, player.Name },
                });
            }
        }

        private static async Task ReviveAsync(Match match, PlayerComponentBundle player)
        {
            await player.OnMapAsync(() =>
            {
                player.InTalentArenaRing = false;
                player.Hp = player.MaxHp;
                player.Mp = player.MaxMp;
                player.IsAlive = true;
                return Task.CompletedTask;
            });
            await match.MapInstance.SendPacketAsync(new RevivePacket { VisualType = VisualType.Player, VisualId = player.VisualId, Data = 0 });
            await player.SendPacketAsync(player.GenerateStat());
        }

        private async Task DropAbsentAsync(Match match)
        {
            foreach (var fighter in match.Fighters.Where(f => !f.Defeated && !f.Left))
            {
                if (Session(fighter.CharacterId) is { } session && session.Character.MapInstanceId == match.MapInstance.MapInstanceId)
                {
                    continue;
                }

                fighter.Left = true;
                if (fighter.InRing)
                {
                    await KnockOutAsync(match, fighter);
                }
                else
                {
                    fighter.Defeated = true;
                }
            }
        }

        private async Task EndAsync(Match match)
        {
            ArenaResultType result;
            await _lock.WaitAsync();
            try
            {
                result = Result(match) ?? ArenaResultType.Draw;
                foreach (var fighter in match.Fighters.Where(f => !f.Left))
                {
                    if (Session(fighter.CharacterId) is not { } session || session.Character.MapInstanceId != match.MapInstance.MapInstanceId)
                    {
                        continue;
                    }

                    session.Character.InTalentArenaRing = false;
                    await session.SendPacketAsync(Scores(match, TaWindowMode.Update));
                    await session.SendPacketAsync(Lists(match, fighter.Team, TaWindowMode.Update));
                    await session.SendPacketAsync(new TaFPacket
                    {
                        Result = result,
                        TeamType = fighter.Team,
                        Won = match.Wins[fighter.Team],
                        LivesLeft = Lives(match, fighter.Team),
                        OpponentWon = match.Wins[Opponent(fighter.Team)],
                        OpponentLivesLeft = Lives(match, Opponent(fighter.Team)),
                    });
                    var won = result == (fighter.Team == ArenaTeamType.Zenas ? ArenaResultType.ZenasWon : ArenaResultType.EreniaWon);
                    await RewardAsync(session.Character, won ? WinnerRewardVNum : LoserRewardVNum,
                        won ? WinnerRewardAmount : LoserRewardAmount);
                }
            }
            finally
            {
                _lock.Release();
            }

            await Task.Delay(ResultTime);
            foreach (var fighter in match.Fighters)
            {
                if (Session(fighter.CharacterId) is not { } session)
                {
                    continue;
                }

                session.Character.TalentArenaTeam = null;
                session.Character.InTalentArenaRing = false;
                if (session.Character.MapInstanceId != match.MapInstance.MapInstanceId)
                {
                    continue;
                }

                await session.SendPacketAsync(Scores(match, TaWindowMode.Close));
                await session.SendPacketAsync(Lists(match, fighter.Team, TaWindowMode.Close));
                await session.SendPacketAsync(new TeamArenaClosePacket());
                await mapChangeService.ChangeMapInstanceAsync(session, fighter.ReturnMapInstanceId, fighter.ReturnX, fighter.ReturnY);
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

        private static ArenaResultType? Result(Match match) => Result(match.Fighters.Select(f => (f.Team, f.Defeated)).ToList());

        private static Fighter? Next(Match match, ArenaTeamType team) =>
            match.Fighters.Where(f => f.Team == team && !f.Defeated).OrderBy(f => f.Order).FirstOrDefault();

        private static int Lives(Match match, ArenaTeamType team) => match.Fighters.Count(f => f.Team == team && !f.Defeated);

        private static ArenaTeamType Opponent(ArenaTeamType team) =>
            team == ArenaTeamType.Zenas ? ArenaTeamType.Erenia : ArenaTeamType.Zenas;

        private IEnumerable<ClientSession> Spectators(Match match) =>
            sessionRegistry.GetClientSessionsByMapInstance(match.MapInstance.MapInstanceId).Where(s => s.HasPlayerEntity);

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

        private static TaMPacket Scores(Match match, TaWindowMode mode, int seconds = 0) => new()
        {
            Type = mode,
            ZenasScore = match.Wins[ArenaTeamType.Zenas],
            EreniaScore = match.Wins[ArenaTeamType.Erenia],
            TimeRemaining = seconds,
        };

        private TaPPacket Lists(Match match, ArenaTeamType team, TaWindowMode mode) => new()
        {
            Type = mode,
            TeamType = team,
            Fighters = Slots(match, team).Select(f => f == null || Session(f.CharacterId)?.Character is not { } p
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

        private TaPsPacket Bars(Match match) => new()
        {
            Fighters = Slots(match, ArenaTeamType.Zenas).Select(f => f == null || Session(f.CharacterId)?.Character is not { } p
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
        private static IEnumerable<Fighter?> Slots(Match match, ArenaTeamType team) =>
            new[] { team, Opponent(team) }.SelectMany(t => Enumerable.Range(0, TalentArenaTeams.TeamSize)
                .Select(order => match.Fighters.Find(f => f.Team == t && f.Order == order)));

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
        }

        private sealed class Match(MapInstance mapInstance, List<Fighter> fighters)
        {
            public MapInstance MapInstance { get; } = mapInstance;

            public List<Fighter> Fighters { get; } = fighters;

            public Dictionary<ArenaTeamType, int> Wins { get; } = new() { [ArenaTeamType.Zenas] = 0, [ArenaTeamType.Erenia] = 0 };

            public bool RoundOver { get; set; }
        }
    }
}

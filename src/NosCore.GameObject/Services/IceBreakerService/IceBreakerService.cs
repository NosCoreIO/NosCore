//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosCore.Algorithm.IceBreakerRewardService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.IceBreakerService
{
    public class IceBreakerService(
        Lazy<IMapInstanceGeneratorService> mapInstanceGeneratorService,
        IMapInstanceRegistry mapInstanceRegistry,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IRandomProvider random,
        IIceBreakerRewardService rewardService,
        IOptions<WorldConfiguration> worldConfiguration,
        ILogger<IceBreakerService> logger) : IIceBreakerService, ISingletonService
    {
        public const short MapId = 2005;
        public const int MaxPlayers = 50;
        public const long EntryFee = 500;
        public const byte FrozenEffect = 35;

        private static readonly (TimeSpan Before, Game18NConstString Delay, int Amount)[] Countdown =
        [
            (TimeSpan.FromMinutes(5), Game18NConstString.InMinutes, 5),
            (TimeSpan.FromMinutes(1), Game18NConstString.InMinutes, 1),
            (TimeSpan.FromSeconds(30), Game18NConstString.InSeconds, 30),
            (TimeSpan.FromSeconds(10), Game18NConstString.InSeconds, 10),
        ];

        // The client formats the delay line with the first two arguments and puts it before the message,
        // which it formats with the last two.
        private const byte DelayThenTwoNumbers = 9;

        private static readonly TimeSpan SignUpTime = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan[] PvpWarnings = [TimeSpan.Zero, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(7)];
        private static readonly TimeSpan BeforeAttack = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan ReturnTime = TimeSpan.FromSeconds(10);

        // Nothing in the client data gives the length of the fight; ten minutes keeps two players who avoid
        // each other from holding the map forever.
        private const int FightSeconds = 600;
        private const int LastWarningSeconds = 30;

        private readonly ConcurrentDictionary<long, byte> _frozen = new();
        private int _running;
        private int _bracket;
        private volatile MapInstance? _signUpMap;

        public bool IsRunning => Volatile.Read(ref _running) == 1;

        public static TimeSpan CountdownLength => Countdown[0].Before;

        public bool TryStart(bool withCountdown)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                return false;
            }

            var bracket = _bracket;
            _bracket = (_bracket + 1) % rewardService.BracketCount;
            _ = Task.Run(async () =>
            {
                try
                {
                    await RunAsync(bracket, withCountdown);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Icebreaker failed");
                }
                finally
                {
                    _signUpMap = null;
                    _frozen.Clear();
                    Volatile.Write(ref _running, 0);
                }
            });
            return true;
        }

        public async Task JoinAsync(ClientSession session)
        {
            if (!session.HasPlayerEntity)
            {
                return;
            }

            var character = session.Character;
            if (_signUpMap is not { } mapInstance)
            {
                await character.SendPacketAsync(Message(Game18NConstString.CanNotJoinStartedIceBreaker));
                return;
            }

            if (character.MapInstance?.MapInstanceType != MapInstanceType.BaseMapInstance || !character.IsAlive
                || mapInstance.Sessions.Count >= MaxPlayers)
            {
                return;
            }

            if (character.Gold < EntryFee)
            {
                await character.SendPacketAsync(Message(Game18NConstString.NotEnoughGold));
                return;
            }

            character.Gold -= EntryFee;
            await character.SendPacketAsync(character.GenerateGold());
            var (x, y) = RandomCell(mapInstance);
            await mapChangeService.ChangeMapInstanceAsync(session, mapInstance.MapInstanceId, x, y);
        }

        public async Task FreezeAsync(PlayerComponentBundle player)
        {
            _frozen.TryAdd(player.CharacterId, 0);
            await player.OnMapAsync(() =>
            {
                player.Hp = player.MaxHp;
                player.Mp = player.MaxMp;
                player.IsAlive = true;
                player.NoMove = true;
                player.NoAttack = true;
                return Task.CompletedTask;
            });
            if (player.MapInstance is { } mapInstance)
            {
                await mapInstance.SendPacketAsync(new RevivePacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.VisualId,
                    Data = 0,
                });
                await mapInstance.SendPacketAsync(new Msgi2Packet
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.IsFrozen,
                    ArgumentType = 1,
                    Game18NArguments = { player.Name },
                });
            }

            await player.SendPacketAsync(player.GenerateStat());
            await player.SendPacketAsync(player.GenerateCond());
        }

        // Group members fight on the same side, so the fight is over once every player left standing
        // is in the same group.
        public static bool IsDecided(IReadOnlyCollection<PlayerComponentBundle> standing)
        {
            return standing.Count <= 1 || standing.All(p => standing.All(o => o.CharacterId == p.CharacterId
                || p.Group.ContainsKey(new Tuple<VisualType, long>(VisualType.Player, o.VisualId))));
        }

        private async Task RunAsync(int bracket, bool withCountdown)
        {
            var levelMinimum = rewardService.GetLevelMinimum(bracket);
            var levelMaximum = rewardService.GetLevelMaximum(bracket);
            if (withCountdown)
            {
                for (var i = 0; i < Countdown.Length; i++)
                {
                    var (before, delay, amount) = Countdown[i];
                    await AnnounceAsync(delay, amount, levelMinimum, levelMaximum);
                    await Task.Delay(i + 1 < Countdown.Length ? before - Countdown[i + 1].Before : before);
                }
            }

            var baseMap = mapInstanceRegistry.GetBaseMapById(MapId);
            if (baseMap == null)
            {
                logger.LogError("Icebreaker needs map {MapId} imported by the parser", MapId);
                return;
            }

            var mapInstance = mapInstanceGeneratorService.Value.CreateMapInstance(baseMap.Map, Guid.NewGuid(), false,
                MapInstanceType.IceBreakerInstance);
            await mapInstanceGeneratorService.Value.AddMapInstanceAsync(mapInstance);
            await mapInstance.StartLifeAsync();
            try
            {
                await BroadcastAsync(Message(Game18NConstString.IcebreakerStarted));
                _signUpMap = mapInstance;
                foreach (var session in sessionRegistry.GetSessions(s => s.HasPlayerEntity
                             && s.Character.Level >= levelMinimum && s.Character.Level <= levelMaximum
                             && s.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance))
                {
                    await session.SendPacketAsync(new QnamliPacket
                    {
                        Type = QnamlType.IceBreaker,
                        YesPacket = new NosCore.Packets.ClientPackets.UI.GuriPacket { Type = GuriPacketType.IceBreaker },
                        Question = Game18NConstString.AskJoinIcebreaker,
                        ArgumentType = 4,
                        Game18NArguments = { EntryFee },
                    });
                }

                await Task.Delay(SignUpTime);
                _signUpMap = null;

                if (Players(mapInstance).Count > 1)
                {
                    foreach (var warning in PvpWarnings)
                    {
                        await Task.Delay(warning);
                        await mapInstance.SendPacketAsync(Message(Game18NConstString.PvpWillBeAvailable));
                    }

                    await Task.Delay(BeforeAttack);
                    await mapInstance.SendPacketAsync(Message(Game18NConstString.BeginAttack));
                    mapInstance.IsPvp = true;
                    await FightAsync(bracket, mapInstance);
                    mapInstance.IsPvp = false;
                }
                else
                {
                    await EndAsync(bracket, mapInstance, Players(mapInstance));
                }

                if (mapInstance.Sessions.Count > 0)
                {
                    await mapInstance.SendPacketAsync(Message(Game18NConstString.ReturnToStartingPosition));
                    await Task.Delay(ReturnTime);
                }
            }
            finally
            {
                _signUpMap = null;
                await ThawAllAsync();
                await mapInstanceGeneratorService.Value.RemoveMapAsync(mapInstance.MapInstanceId);
            }
        }

        private async Task FightAsync(int bracket, MapInstance mapInstance)
        {
            for (var second = 0; second < FightSeconds; second++)
            {
                var players = Players(mapInstance);
                if (players.Count == 0)
                {
                    return;
                }

                var standing = players.Where(p => !_frozen.ContainsKey(p.CharacterId)).ToList();
                if (IsDecided(standing))
                {
                    await EndAsync(bracket, mapInstance, standing);
                    return;
                }

                foreach (var frozen in players.Where(p => _frozen.ContainsKey(p.CharacterId)))
                {
                    await mapInstance.SendPacketAsync(frozen.GenerateEff(FrozenEffect));
                }

                if (second == FightSeconds - LastWarningSeconds)
                {
                    await mapInstance.SendPacketAsync(Message(Game18NConstString.IcebreakerWillEndInThirtySeconds));
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            await mapInstance.SendPacketAsync(Message(Game18NConstString.IcebreakerTimeElapsed));
        }

        private async Task EndAsync(int bracket, MapInstance mapInstance, IReadOnlyCollection<PlayerComponentBundle> winners)
        {
            if (winners.Count == 0)
            {
                await mapInstance.SendPacketAsync(Message(Game18NConstString.EverybodyIsFrozen));
                return;
            }

            await mapInstance.SendPacketAsync(Message(Game18NConstString.IceBreakerOver));
            foreach (var winner in winners)
            {
                await RewardAsync(bracket, winner);
            }
        }

        private async Task RewardAsync(int bracket, PlayerComponentBundle character)
        {
            var gold = rewardService.GetGold(bracket);
            character.Gold = Math.Min(worldConfiguration.Value.MaxGoldAmount, character.Gold + gold);
            await character.SendPacketAsync(character.GenerateGold());
            if (character.Dignity < 100)
            {
                character.Dignity = 100;
            }

            await character.SetReputationAsync(character.Reputation + rewardService.GetReputation(character.Level));
            await character.SendPacketAsync(new MsgiPacket
            {
                Message = Game18NConstString.EarnedReputationAndReward,
                ArgumentType = 4,
                Game18NArguments = { gold },
            });
        }

        private async Task ThawAllAsync()
        {
            foreach (var characterId in _frozen.Keys)
            {
                _frozen.TryRemove(characterId, out _);
                if (sessionRegistry.GetSessionByCharacterId(characterId) is not { HasPlayerEntity: true } session)
                {
                    continue;
                }

                var character = session.Character;
                await character.OnMapAsync(() =>
                {
                    character.NoMove = false;
                    character.NoAttack = false;
                    return Task.CompletedTask;
                });
                await character.SendPacketAsync(character.GenerateCond());
            }
        }

        private List<PlayerComponentBundle> Players(MapInstance mapInstance)
        {
            return sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId)
                .Where(s => s.HasPlayerEntity)
                .Select(s => s.Character)
                .ToList();
        }

        private (short X, short Y) RandomCell(MapInstance mapInstance)
        {
            return mapInstance.Map.RandomWalkableCell(random, 100) ?? (0, 0);
        }

        private static MsgiPacket Message(Game18NConstString message) => new() { Message = message };

        private async Task AnnounceAsync(Game18NConstString delay, int amount, byte levelMinimum, byte levelMaximum)
        {
            foreach (var type in new[] { MessageType.Default, MessageType.Notification })
            {
                await sessionRegistry.BroadcastPacketAsync(new MsgiPacket
                {
                    Type = type,
                    Message = Game18NConstString.IceBreakerWillStartForLevels,
                    ArgumentType = DelayThenTwoNumbers,
                    Game18NArguments = { (int)delay, amount, levelMinimum, levelMaximum },
                });
            }
        }

        private async Task BroadcastAsync(MsgiPacket message)
        {
            await sessionRegistry.BroadcastPacketAsync(message);
            await sessionRegistry.BroadcastPacketAsync(new MsgiPacket
            {
                Type = MessageType.Notification,
                Message = message.Message,
            });
        }
    }
}

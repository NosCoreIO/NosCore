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
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
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
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.IceBreakerService
{
    public class IceBreakerService(
        IScriptedInstanceService scriptedInstanceService,
        IMapInstanceRegistry mapInstanceRegistry,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IRandomProvider random,
        IIceBreakerRewardService rewardService,
        IOptions<WorldConfiguration> worldConfiguration,
        ILogger<IceBreakerService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IIceBreakerService, ISingletonService
    {
        public const string Script = "ice-breaker";
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

        private int _running;
        private IInstanceScript? _script;
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
                    logger.LogError(e, logLanguage[LogLanguageKey.ICE_BREAKER_FAILED]);
                }
                finally
                {
                    _signUpMap = null;
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

            _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
            if (_script is not { } script)
            {
                logger.LogError(logLanguage[LogLanguageKey.ICE_BREAKER_SCRIPT_MISSING], Script);
                return;
            }

            var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.IceBreakerInstance,
                new RunHost(this, bracket));
            if (run == null || mapInstanceRegistry.GetById(run.Rooms[script.Definition.Rooms[0].Key]) is not { } mapInstance)
            {
                return;
            }

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
            if (!await scriptedInstanceService.DisposeIfEmptyAsync(run))
            {
                await run.StartAsync();
            }
        }

        private sealed class RunHost(IceBreakerService service, int bracket) : IInstanceRunHost
        {
            public string Name => service.NameOf(bracket);

            public bool StartsOnFirstEntry => false;

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player) =>
                service.FreezeAsync(player);

            public async Task TickAsync(ScriptedInstanceRun run)
            {
                foreach (var session in run.Players().Where(s => s.HasPlayerEntity && run.IsDown(s.Character.CharacterId)).ToList())
                {
                    await run.BroadcastAsync(session.Character.GenerateEff(FrozenEffect));
                }
            }

            public async Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
            {
                if (state == EndStateType.Success)
                {
                    var winners = run.Players()
                        .Where(s => s.HasPlayerEntity && !run.IsDown(s.Character.CharacterId))
                        .Select(s => s.Character)
                        .ToList();
                    await run.BroadcastAsync(Message(winners.Count == 0
                        ? Game18NConstString.EverybodyIsFrozen
                        : Game18NConstString.IceBreakerOver));
                    foreach (var winner in winners)
                    {
                        await service.RewardAsync(bracket, winner);
                    }
                }

                await run.BroadcastAsync(Message(Game18NConstString.ReturnToStartingPosition));
            }

            public Task ReturnAsync(ClientSession session) => service.ReturnAsync(session);
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

        private string NameOf(int bracket) => $"Icebreaker {rewardService.GetLevelMinimum(bracket)}+";

        private async Task ReturnAsync(ClientSession session)
        {
            await ThawAsync(session.Character);
            await mapChangeService.ChangeMapAsync(session, session.Character.MapId, session.Character.MapX,
                session.Character.MapY);
        }

        private async Task ThawAsync(PlayerComponentBundle character)
        {
            await character.OnMapAsync(() =>
            {
                character.NoMove = false;
                character.NoAttack = false;
                return Task.CompletedTask;
            });
            await character.SendPacketAsync(character.GenerateCond());
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

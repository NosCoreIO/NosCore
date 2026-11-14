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
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.InstantCombatService
{
    public class InstantCombatService(
        List<InstantCombatMonsterDto> monsters,
        List<InstantCombatDropDto> drops,
        List<NpcMonsterDto> npcMonsters,
        Lazy<IMapInstanceGeneratorService> mapInstanceGeneratorService,
        IMapInstanceRegistry mapInstanceRegistry,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IItemGenerationService itemGenerationService,
        IMapItemGenerationService mapItemGenerationService,
        IRandomProvider random,
        IInstantCombatRewardService rewardService,
        IFamilyContributionService familyContributionService,
        IOptions<WorldConfiguration> worldConfiguration,
        ILogger<InstantCombatService> logger) : IInstantCombatService, ISingletonService
    {
        public const short MapId = 2004;
        public const int PlayersPerRoom = 50;
        public const byte LastWave = 4;

        private static readonly (TimeSpan Before, Game18NConstString Delay, int Amount)[] Countdown =
        [
            (TimeSpan.FromMinutes(5), Game18NConstString.InMinutes, 5),
            (TimeSpan.FromMinutes(1), Game18NConstString.InMinutes, 1),
            (TimeSpan.FromSeconds(30), Game18NConstString.InSeconds, 30),
            (TimeSpan.FromSeconds(10), Game18NConstString.InSeconds, 10),
        ];

        // The client formats the delay line with the first two arguments and puts it before the message.
        private const byte DelayThenMessage = 10;

        private static readonly TimeSpan SignUpTime = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ArrivalTime = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ReturnTime = TimeSpan.FromSeconds(10);

        // Seconds into the fight: each wave lands 160 s after the previous one, its leftovers are
        // scattered 130 s after it lands, and the fight can be won once the last wave is out.
        private const int FirstWaveSecond = 10;
        private const int WaveInterval = 160;
        private const int DropDelay = 130;
        private const int WarningBeforeWave = 10;
        private const int WinnableFromSecond = 720;
        private const int ThirtySecondsLeftSecond = 870;
        private const int DurationSeconds = 900;

        private int _running;
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
                    logger.LogError(e, "Instant Combat failed");
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

        public static int WaveSecond(int wave) => FirstWaveSecond + wave * WaveInterval;

        private async Task RunAsync(bool withCountdown)
        {
            if (withCountdown)
            {
                for (var i = 0; i < Countdown.Length; i++)
                {
                    var (before, delay, amount) = Countdown[i];
                    await AnnounceAsync(delay, amount);
                    await Task.Delay(i + 1 < Countdown.Length ? before - Countdown[i + 1].Before : before);
                }
            }

            await BroadcastAsync(new MsgiPacket { Message = Game18NConstString.InstantCombatStarted });
            _signUpOpen = true;
            await sessionRegistry.BroadcastPacketAsync(new QnamliPacket
            {
                Type = QnamlType.InstantCombat,
                YesPacket = new NosCore.Packets.ClientPackets.UI.GuriPacket { Type = GuriPacketType.EventInWaiting },
                Question = Game18NConstString.JoinInstantCombatQuestion,
            });
            await Task.Delay(SignUpTime);
            _signUpOpen = false;

            var players = sessionRegistry.GetSessions(s => s.HasPlayerEntity).ToList();
            var joining = players.Where(s => s.Character.IsWaitingForEvent
                && s.Character.IsAlive
                && s.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance).ToList();
            foreach (var session in players)
            {
                if (!session.Character.IsWaitingForEvent)
                {
                    await session.SendPacketAsync(new EsfPacket());
                }

                session.Character.IsWaitingForEvent = false;
            }

            var baseMap = mapInstanceRegistry.GetBaseMapById(MapId);
            var brackets = monsters.Select(m => m.LevelMinimum).Distinct().ToList();
            if (baseMap == null || brackets.Count == 0)
            {
                logger.LogError("Instant Combat needs map {MapId} and its waves imported by the parser", MapId);
                return;
            }

            var rooms = new List<Task>();
            foreach (var room in GroupIntoRooms(joining, brackets))
            {
                var mapInstance = mapInstanceGeneratorService.Value.CreateMapInstance(baseMap.Map, Guid.NewGuid(), false,
                    MapInstanceType.InstantCombatInstance);
                await mapInstanceGeneratorService.Value.AddMapInstanceAsync(mapInstance);
                await mapInstance.StartLifeAsync();
                foreach (var player in room.Players)
                {
                    var (x, y) = RandomCell(mapInstance);
                    await mapChangeService.ChangeMapInstanceAsync(player, mapInstance.MapInstanceId, x, y);
                }

                rooms.Add(RunRoomAsync(room.LevelMinimum, mapInstance));
            }

            await Task.WhenAll(rooms);
        }

        private async Task RunRoomAsync(byte levelMinimum, MapInstance mapInstance)
        {
            try
            {
                await Task.Delay(ArrivalTime);
                var won = false;
                for (var second = 0; second < DurationSeconds; second++)
                {
                    if (mapInstance.Sessions.Count == 0)
                    {
                        return;
                    }

                    await TickAsync(levelMinimum, mapInstance, second);
                    if (second >= WinnableFromSecond && !mapInstance.EnumerateMonsters().Any(m => m.IsAlive))
                    {
                        won = true;
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                if (won)
                {
                    await mapInstance.SendPacketAsync(Message(Game18NConstString.InstantCombatSuccess));
                    foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId)
                                 .Where(s => s.HasPlayerEntity && s.Character.IsAlive))
                    {
                        await RewardAsync(session.Character);
                    }
                }
                else
                {
                    await mapInstance.SendPacketAsync(Message(Game18NConstString.InstantCombatElapsed));
                }

                await mapInstance.SendPacketAsync(Message(Game18NConstString.ReturnToStartingPositionTenSeconds));
                await Task.Delay(ReturnTime);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Instant Combat room {MapInstanceId} failed", mapInstance.MapInstanceId);
            }
            finally
            {
                await mapInstanceGeneratorService.Value.RemoveMapAsync(mapInstance.MapInstanceId);
            }
        }

        private async Task TickAsync(byte levelMinimum, MapInstance mapInstance, int second)
        {
            for (byte wave = 0; wave <= LastWave; wave++)
            {
                var waveSecond = WaveSecond(wave);
                if (second == waveSecond - WarningBeforeWave)
                {
                    await mapInstance.SendPacketAsync(Message(Game18NConstString.MonstersAreApproaching));
                }
                else if (second == waveSecond)
                {
                    await SpawnWaveAsync(levelMinimum, wave, mapInstance);
                    await mapInstance.SendPacketAsync(Message(Game18NConstString.MonsterHaveAppeared));
                }
                else if (second == waveSecond + DropDelay)
                {
                    await DropAsync(levelMinimum, wave, mapInstance);
                }
            }

            if (second == ThirtySecondsLeftSecond)
            {
                await mapInstance.SendPacketAsync(Message(Game18NConstString.InstantCombatThirtySecondsRemaining));
            }
        }

        private async Task SpawnWaveAsync(byte levelMinimum, byte wave, MapInstance mapInstance)
        {
            foreach (var line in monsters.Where(m => m.LevelMinimum == levelMinimum && m.Wave == wave))
            {
                var npcMonster = npcMonsters.Find(n => n.NpcMonsterVNum == line.NpcMonsterVNum);
                if (npcMonster == null)
                {
                    continue;
                }

                for (var i = 0; i < line.Amount; i++)
                {
                    var (x, y) = RandomCell(mapInstance);
                    var monster = mapInstance.SpawnMonster(npcMonster, x, y, line.IsMoving);
                    await mapInstance.SendPacketAsync(monster.GenerateIn());
                }
            }
        }

        private async Task DropAsync(byte levelMinimum, byte wave, MapInstance mapInstance)
        {
            foreach (var line in drops.Where(d => d.LevelMinimum == levelMinimum && d.Wave == wave))
            {
                for (var i = 0; i < line.Piles; i++)
                {
                    var (x, y) = RandomCell(mapInstance);
                    var item = itemGenerationService.Create(line.ItemVNum, line.Amount);
                    var dropped = mapItemGenerationService.Create(mapInstance, item, x, y);
                    await mapInstance.SendPacketAsync(dropped.GenerateDrop());
                }
            }
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

        private static MsgiPacket Message(Game18NConstString message) => new() { Message = message };

        private async Task AnnounceAsync(Game18NConstString delay, int amount)
        {
            foreach (var type in new[] { MessageType.Default, MessageType.Notification })
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

        private async Task BroadcastAsync(MsgiPacket message)
        {
            message.Type = MessageType.Default;
            await sessionRegistry.BroadcastPacketAsync(message);
            await sessionRegistry.BroadcastPacketAsync(new MsgiPacket
            {
                Type = MessageType.Notification,
                Message = message.Message,
                ArgumentType = message.ArgumentType,
                Game18NArguments = message.Game18NArguments,
            });
        }
    }
}

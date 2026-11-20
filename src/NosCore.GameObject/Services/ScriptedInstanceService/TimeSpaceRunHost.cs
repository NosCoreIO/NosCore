//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.TimeSpacePenaltyService;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class TimeSpaceRunHost(ScriptedInstance entrance, Lazy<IMapChangeService> mapChangeService,
        IGameEventDispatcher gameEvents, ITimeSpacePenaltyService timeSpacePenaltyService)
        : IInstanceRunHost
    {
        // Every time-space revival in the packet traces names 568 for a level 14-19 character and 569 for a
        // level 76 one; the level where one gives way to the other is not shown, so it follows the map's 20.
        private const byte HighestLevelForTheLowLevelRevival = 20;

        private int _livesLost;

        public ScriptedInstance Entrance => entrance;

        public string Name => $"scripted instance {entrance.ScriptedInstanceId}";

        public int? LivesLeft => Math.Max(0, (entrance.Definition?.Lives ?? 0) - _livesLost);

        public bool DrawsRoomMap => true;

        public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

        public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

        public async Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
        {
            var participants = Participants(run);
            if (state == EndStateType.Timeout)
            {
                // A run that ran out of time scores nothing.
                await run.BroadcastAsync(new ScorePacket { EndState = state });
                await RaiseFailedAsync(participants);
                return;
            }

            if (state != EndStateType.Success)
            {
                await run.BroadcastAsync(Score(run, state, run.Points));
                await RaiseFailedAsync(participants);
                return;
            }

            foreach (var character in participants)
            {
                var penalty = timeSpacePenaltyService.GetPointPenalty(character.Level, entrance.EffectiveLevelMinimum);
                if (penalty > 0)
                {
                    await character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow,
                        Game18NConstString.PointsReducedLevelPenalty, 4, penalty));
                }

                await character.SendPacketAsync(Score(run, state, run.Points * (100 - penalty) / 100));
            }

            if (entrance.Definition is not { } definition)
            {
                return;
            }

            foreach (var session in run.Players().Where(s => s.HasPlayerEntity).ToList())
            {
                if (entrance.StoneVNum is { } stone)
                {
                    await session.Character.SendPacketsAsync(session.Character.InventoryService.RemoveItemAmountByVNum(stone, 1));
                }

                await gameEvents.RaiseAsync(new ScriptedInstanceSucceededEvent(session.Character, entrance.Type, definition.Id));
            }
        }

        private ScorePacket Score(ScriptedInstanceRun run, EndStateType state, int points) => new()
        {
            EndState = state,
            Points = points,
            DrawItemsCount = (byte)(entrance.Definition?.DrawItems.Count ?? 0),
            MonsterKilled = (short)run.MonstersKilled,
            RemainingNpc = (byte)run.NpcsSummoned,
            RoomsVisited = (short)run.RoomsVisited,
            IsAllMonstersKilled = run.MonstersKilled >= run.MonstersSummoned,
            IsNoNpcDead = true
        };

        // Taken before any await, so a player leaving mid-loop does not cut the others' events short.
        private static List<PlayerComponentBundle> Participants(ScriptedInstanceRun run) =>
            [.. run.Players().Where(s => s.HasPlayerEntity).Select(s => s.Character)];

        private async Task RaiseFailedAsync(List<PlayerComponentBundle> participants)
        {
            if (entrance.Definition is not { } definition)
            {
                return;
            }

            foreach (var character in participants)
            {
                await gameEvents.RaiseAsync(new TimeSpaceFailedEvent(character, definition.Id));
            }
        }

        public async Task ReturnAsync(ClientSession session)
        {
            if (!session.Character.IsAlive)
            {
                await session.Character.ReviveAsync(fully: false);
            }

            await mapChangeService.Value.ChangeMapAsync(session, entrance.MapId, entrance.PositionX, entrance.PositionY);
        }

        // Lives are shared by the whole run, and the death that uses the last one fails it. False then.
        public bool LoseLife(ScriptedInstanceRun run)
        {
            _livesLost++;
            if (_livesLost < (entrance.Definition?.Lives ?? 0))
            {
                return true;
            }

            run.RunOutOfLives();
            return false;
        }

        public async Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player)
        {
            if (!LoseLife(run))
            {
                return;
            }

            await player.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.YouCanBeRevivedMoreTime,
                ArgumentType = 4,
                Game18NArguments = { (entrance.Definition?.Lives ?? 0) - _livesLost }
            });
            await player.SendPacketAsync(player.GenerateStat());
            // Both answers carry the same revival: the run brings the player back where they fell.
            await player.SendPacketAsync(new DlgiPacket
            {
                YesPacket = new RevivalPacket { Type = RevivalType.AtSavePoint },
                NoPacket = new RevivalPacket { Type = RevivalType.AtSavePoint },
                Question = player.Level <= HighestLevelForTheLowLevelRevival
                    ? Game18NConstString.MissionContinueLessThanLevel10
                    : Game18NConstString.MissionContinueHalfHpMp,
            });
        }

        public async Task RevivalAnsweredAsync(ScriptedInstanceRun run, PlayerComponentBundle player)
        {
            run.StoodUp(player.CharacterId);
            await player.ReviveInPlaceAsync(percent: 50);
        }
    }
}

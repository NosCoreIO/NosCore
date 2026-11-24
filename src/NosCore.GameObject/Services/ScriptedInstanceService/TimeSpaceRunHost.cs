//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Algorithm.LowLevelBoostService;
using NosCore.Algorithm.TimeSpaceExperienceService;
using NosCore.Algorithm.TimeSpacePenaltyService;
using NosCore.Algorithm.TimeSpaceScoreService;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class TimeSpaceRunHost(ScriptedInstance entrance, Lazy<IMapChangeService> mapChangeService,
        IGameEventDispatcher gameEvents, ITimeSpacePenaltyService timeSpacePenaltyService,
        ITimeSpaceExperienceService timeSpaceExperienceService, ITimeSpaceScoreService timeSpaceScoreService,
        ILowLevelBoostService lowLevelBoostService,
        IExperienceBonusService experienceBonusService, IExperienceProgressionService experienceProgression,
        ITimeSpaceRecordService timeSpaceRecordService, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
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

        public bool AsksToStart => true;

        public bool ShowsScore => true;

        public int KillPoints(byte monsterLevel, byte killerLevel, int chain) =>
            timeSpaceScoreService.GetKillPoints(monsterLevel, killerLevel, chain);

        public int RoomClearedPoints => 50;

        public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

        public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

        public async Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
        {
            var participants = Participants(run);
            if (state != EndStateType.Success)
            {
                await run.BroadcastAsync(new ScoreFailedPacket { EndState = state });
                await RaiseFailedAsync(participants);
                return;
            }

            // A participant's rate is that of the room they cleared, even if they leave while others are rewarded.
            foreach (var (character, rate) in participants.Select(c => (c, c.MapInstance?.XpRate ?? 1)).ToList())
            {
                var experience = timeSpaceExperienceService.GetExperience(entrance.EffectiveLevelMinimum, run.MonstersKilled);
                var jobExperience = timeSpaceExperienceService.GetJobExperience(run.MonstersKilled);
                await RewardStepAsync(character, () => SendScoreAsync(run, character, experience, jobExperience));
                await RewardStepAsync(character, () => AwardExperienceAsync(character, experience, jobExperience, rate));
                await RewardStepAsync(character, () => TakeStoneAsync(character));
                await RewardStepAsync(character, () => RecordClearAsync(character));
            }
        }

        // Each step of a participant's reward stands on its own, so one that fails costs neither the
        // participant's other steps nor the other participants anything.
        private async Task RewardStepAsync(PlayerComponentBundle character, Func<Task> step)
        {
            try
            {
                await step();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, logLanguage[LogLanguageKey.TIME_SPACE_CLEAR_REWARD_FAILED], Name, character.CharacterId);
            }
        }

        private async Task SendScoreAsync(ScriptedInstanceRun run, PlayerComponentBundle character, int experience, int jobExperience)
        {
            var penalty = timeSpacePenaltyService.GetPointPenalty(character.Level, entrance.EffectiveLevelMinimum);
            if (penalty > 0)
            {
                await character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow,
                    Game18NConstString.PointsReducedLevelPenalty, 4, penalty));
            }

            var points = run.Score * (100 - penalty) / 100;
            await character.SendPacketAsync(Score(run, character, points, experience, jobExperience));
            if (entrance.Definition is { } definition
                && await timeSpaceRecordService.TryBreakAsync(definition.Id, character.CharacterId, points))
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.BrokeRecordScore
                });
            }
        }

        private Task TakeStoneAsync(PlayerComponentBundle character) =>
            entrance.Definition != null && entrance.StoneVNum is { } stone
                ? character.SendPacketsAsync(character.InventoryService.RemoveItemAmountByVNum(stone, 1))
                : Task.CompletedTask;

        private Task RecordClearAsync(PlayerComponentBundle character) =>
            entrance.Definition is { } definition
                ? gameEvents.RaiseAsync(new ScriptedInstanceSucceededEvent(character, entrance.Type, definition.Id))
                : Task.CompletedTask;

        // Read before the succeeded event records this clear, so a first clear still reads as one.
        private ScorePacket Score(ScriptedInstanceRun run, PlayerComponentBundle character, int points,
            int experience, int jobExperience) => new()
        {
            Points = points,
            Experience = experience,
            JobExperience = jobExperience,
            Gold = (int)Math.Min(entrance.Definition?.GoldFor(run.MonstersKilled) ?? 0, int.MaxValue),
            DrawRewardCount = run.IsLoserMode ? (byte)0 : (byte)(entrance.Definition?.DrawItems.Count ?? 0),
            MonstersKilled = (short)run.MonstersKilled,
            RoomsExplored = (short)run.RoomsVisited,
            NpcsLeft = (short)(run.NpcsSummoned - run.NpcsLost),
            // A category the run had none of is not marked perfect.
            Perfect =
            [
                run.MonstersSummoned > 0 && run.MonstersKilled >= run.MonstersSummoned,
                run.RoomsVisited >= run.Rooms.Count,
                run.NpcsSummoned > 0 && run.NpcsLost == 0
            ],
            ClearType = run.IsLoserMode ? TimeSpaceClearType.LoserMode
                : entrance.Definition is { } definition && character.CompletedTimeSpaces?.Contains(definition.Id) == true
                    ? TimeSpaceClearType.ClearedBefore
                    : TimeSpaceClearType.FirstClear,
            // Captured runs carry 1 up to level 20 and 15 at levels 76 and 98, whatever the time-space or
            // clear type. No capture lies between, so where it changes from 1 to 15 is not known.
            Unknown = character.Level <= 20 ? (byte)1 : (byte)15
        };

        // The score shows the base values; the rate, low-level boost and bonuses apply only to what is gained.
        // Experience changes on the player's map, as a kill's does, so the two never write over each other.
        // A player who moved while waiting for a map is followed to the next one.
        private async Task AwardExperienceAsync(PlayerComponentBundle character, int experience, int jobExperience, int rate)
        {
            while (character.MapInstance is { } map)
            {
                var awarded = await map.Executor.RunAsync(async () =>
                {
                    if (character.MapInstance != map)
                    {
                        return false;
                    }

                    await AddExperienceAsync(character, experience, jobExperience, rate);
                    return true;
                });
                if (awarded)
                {
                    return;
                }
            }

            await AddExperienceAsync(character, experience, jobExperience, rate);
        }

        private Task AddExperienceAsync(PlayerComponentBundle character, int experience, int jobExperience, int rate)
        {
            var bonus = experienceBonusService.GetBonus(character);
            return experienceProgression.AddExperienceAsync(character,
                bonus.ApplyToExperience((long)experience * lowLevelBoostService.GetExperienceBoost(character.Level) * rate),
                bonus.ApplyToJobExperience((long)jobExperience * lowLevelBoostService.GetJobExperienceBoost(character.Level) * rate),
                0, 0, 0);
        }

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

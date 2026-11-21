//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceLevelPenaltyTests
    {
        private const byte LevelMinimum = 10;
        private const short Grunt = 58;

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _room;
        private ScriptedInstanceRun _run;

        private sealed class Script : InstanceScript
        {
            private readonly bool _succeeds;

            public Script(bool succeeds)
            {
                _succeeds = succeeds;
                Definition = new ScriptedInstanceDefinition { Id = 1 };
            }

            public override void OnExitReached(IInstanceRun run, int room)
            {
                if (_succeeds)
                {
                    run.Succeed();
                }
                else
                {
                    run.Fail();
                }
            }
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.UseReal<IExperienceProgressionService, ExperienceProgressionService>();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task AnOverLevelledCharacterLosesTwoPercentPerLevel()
        {
            await new Spec("A character nine levels above the minimum is told of an 18% penalty and scores 82 of 100 points")
                .Given(ACharacterOfLevel_, (byte)19)
                .And(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 25)
                .WhenAsync(TheExitIsReached)
                .Then(ThePenaltyOf_ShouldBeAnnouncedBeforeTheScore, (byte)18)
                .And(TheScoreShouldShow_Points, 82)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFarOverLevelledCharacterScoresNothing()
        {
            await new Spec("A character fifty levels above the minimum is told of a 100% penalty and scores no points")
                .Given(ACharacterOfLevel_, (byte)60)
                .And(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 25)
                .WhenAsync(TheExitIsReached)
                .Then(ThePenaltyOf_ShouldBeAnnouncedBeforeTheScore, (byte)100)
                .And(TheScoreShouldShow_Points, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFarOverLevelledCharacterStillGainsTheWholeExperience()
        {
            await new Spec("The level penalty takes every point but none of the 750 experience and 131 job experience of 25 kills")
                .Given(ACharacterOfLevel_, (byte)60)
                .And(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 25)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldShow_Points, 0)
                .And(TheCharacterShouldHave_ExperienceAnd_JobExperience, 750L, 131L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACharacterAtTheMinimumKeepsEveryPoint()
        {
            await new Spec("A character at the minimum level is told nothing and keeps every point")
                .Given(ACharacterOfLevel_, LevelMinimum)
                .And(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 25)
                .WhenAsync(TheExitIsReached)
                .Then(NoPenaltyShouldBeAnnounced)
                .And(TheScoreShouldShow_Points, 100)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedRunAnnouncesNoPenalty()
        {
            await new Spec("A failed run tells an over-levelled character nothing about a penalty")
                .Given(ACharacterOfLevel_, (byte)19)
                .And(ATimeSpaceThatFails)
                .AndAsync(_MonstersAreKilled, 25)
                .WhenAsync(TheExitIsReached)
                .Then(NoPenaltyShouldBeAnnounced)
                .ExecuteAsync();
        }

        private void ACharacterOfLevel_(byte level)
        {
            _session.Character.Level = level;
            _session.Character.JobLevel = 10;
        }

        private void ATimeSpaceThatSucceeds() => ATimeSpace(true);

        private void ATimeSpaceThatFails() => ATimeSpace(false);

        private void ATimeSpace(bool succeeds)
        {
            _room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            var script = new Script(succeeds);
            _run = new ScriptedInstanceRun(
                World.CreateTimeSpaceRunHost(new GameObject.Services.ScriptedInstanceService.ScriptedInstance
                    {
                        ScriptedInstanceId = 1, MapId = 1, LevelMinimum = LevelMinimum, Logic = script
                    }),
                script,
                new Dictionary<int, MapInstance> { [1] = _room },
                new NpcMonsterCatalog([]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            _session.Character.MapInstance = _room;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, _room.MapInstanceId);
        }

        private async Task _MonstersAreKilled(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await _run.MonsterKilledAsync(_room.MapInstanceId, Grunt);
            }
        }

        private Task TheExitIsReached() => _run.ExitReachedAsync(_room.MapInstanceId);

        private void ThePenaltyOf_ShouldBeAnnouncedBeforeTheScore(byte percent)
        {
            var packets = _session.LastPackets.ToList();
            var penalty = packets.OfType<SayiPacket>().Single(p => p.Message == Game18NConstString.PointsReducedLevelPenalty);
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.PointsReducedLevelPenalty,
                ArgumentType = 4,
                Game18NArguments = { percent }
            }, penalty);
            Assert.IsTrue(packets.IndexOf(penalty) < packets.IndexOf(packets.OfType<ScorePacket>().Single()));
        }

        private void TheCharacterShouldHave_ExperienceAnd_JobExperience(long experience, long jobExperience)
        {
            var character = _session.Character;
            PacketAssert.AreEqual(new LevPacket
            {
                Level = character.Level,
                LevelXp = experience,
                JobLevel = character.JobLevel,
                JobLevelXp = jobExperience,
                XpLoad = World.Create<IExperienceService>().GetExperience(character.Level),
                JobXpLoad = World.Create<IJobExperienceService>().GetJobExperience(character.Class, character.JobLevel),
                Reputation = character.Reputation,
                SkillCp = character.RemainingSkillPoints(World.Create<ISkillPointService>())
            }, _session.LastPackets.OfType<LevPacket>().Single());
        }

        private void NoPenaltyShouldBeAnnounced() =>
            Assert.IsFalse(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.PointsReducedLevelPenalty));

        private void TheScoreShouldShow_Points(int points)
        {
            var score = _session.LastPackets.OfType<ScorePacket>().Single();
            PacketAssert.AreEqual(new ScorePacket
            {
                Points = points,
                Experience = 750,
                JobExperience = 131,
                MonstersKilled = 25,
                Perfect = [false, false, false],
                ClearType = TimeSpaceClearType.FirstClear,
                Unknown = score.Unknown
            }, score);
        }
    }
}

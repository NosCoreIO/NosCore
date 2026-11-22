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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.TimeSpace;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceScoreTests
    {
        private const short TimeSpaceId = 7;
        private const byte LevelMinimum = 10;
        private const byte JobLevel = 10;
        private const short Grunt = 58;
        private const short Villager = 59;
        private const long Gold = 7462;
        private const short Stone = 1184;

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _room;
        private ScriptedInstanceRun _run;
        private readonly Mock<IGameEventDispatcher> _gameEvents = new();
        private Mock<IExperienceProgressionService> _progression;
        private ClientSession _second;
        private long _awarded;

        private sealed class Script : InstanceScript
        {
            private readonly bool _succeeds;
            private readonly bool _hunts;

            public Script(bool succeeds, bool hunts)
            {
                _succeeds = succeeds;
                _hunts = hunts;
                Definition = new ScriptedInstanceDefinition
                {
                    Id = TimeSpaceId,
                    Gold = Gold,
                    DrawItems = [new InstanceGift(1, 1), new InstanceGift(2, 1), new InstanceGift(3, 1)]
                };
            }

            public override void OnFirstEnter(IInstanceRun run)
            {
                run.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)), new MonsterSpawn(Grunt, new Spot(2, 1)));
                run.SummonNpcs(1, new NpcSpawn(Villager, new Spot(1, 2)), new NpcSpawn(Villager, new Spot(2, 2)));
                if (_hunts)
                {
                    run.Hunt(1, Grunt);
                }
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
            World.Inject(_gameEvents.Object);
            _session = await World.CreateSessionAsync();
            _session.Character.Level = LevelMinimum;
            _session.Character.JobLevel = JobLevel;
        }

        [TestMethod]
        public async Task AFirstClearShowsEveryCategoryPerfect()
        {
            await new Spec("A first clear that kills every monster and keeps every NPC alive scores a perfect first clear")
                .GivenAsync(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 2)
                .And(TheClearIsRecordedWhenItSucceeds)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 8,
                    Experience = 60,
                    JobExperience = 10,
                    Gold = (int)Gold,
                    DrawRewardCount = 3,
                    MonstersKilled = 2,
                    RoomsExplored = 1,
                    NpcsLeft = 2,
                    Perfect = [true, true, true],
                    ClearType = TimeSpaceClearType.FirstClear,
                    Unknown = 1
                })
                .And(TheClearShouldBeRecorded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARepeatedClearIsMarkedClearedBefore()
        {
            await new Spec("A time-space the character cleared before scores as a repeat")
                .Given(TheCharacterHasClearedItBefore)
                .AndAsync(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 8,
                    Experience = 60,
                    JobExperience = 10,
                    Gold = (int)Gold,
                    DrawRewardCount = 3,
                    MonstersKilled = 2,
                    RoomsExplored = 1,
                    NpcsLeft = 2,
                    Perfect = [true, true, true],
                    ClearType = TimeSpaceClearType.ClearedBefore,
                    Unknown = 1
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALoserModeClearHasNoDraw()
        {
            await new Spec("A Loser Mode clear scores as Loser Mode with no draw, even on a first clear")
                .GivenAsync(ATimeSpaceThatSucceeds)
                .And(TheRunIsInLoserMode)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 8,
                    Experience = 60,
                    JobExperience = 10,
                    Gold = (int)Gold,
                    DrawRewardCount = 0,
                    MonstersKilled = 2,
                    RoomsExplored = 1,
                    NpcsLeft = 2,
                    Perfect = [true, true, true],
                    ClearType = TimeSpaceClearType.LoserMode,
                    Unknown = 1
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenNpcAndASurvivingMonsterAreNotPerfect()
        {
            await new Spec("A clear that leaves a monster alive and loses an NPC counts the NPCs left and marks only the rooms perfect")
                .GivenAsync(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 1)
                .AndAsync(AnNpcFalls)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 4,
                    Experience = 30,
                    JobExperience = 5,
                    Gold = (int)Gold,
                    DrawRewardCount = 3,
                    MonstersKilled = 1,
                    RoomsExplored = 1,
                    NpcsLeft = 1,
                    Perfect = [false, true, false],
                    ClearType = TimeSpaceClearType.FirstClear,
                    Unknown = 1
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHuntingMonsterLeftAliveKeepsTheMonstersFromBeingPerfect()
        {
            await new Spec("A clear that kills the two monsters set in the room but not the one hunting the player is not perfect on monsters")
                .GivenAsync(ATimeSpaceWithAHunterThatSucceeds)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 8,
                    Experience = 60,
                    JobExperience = 10,
                    Gold = (int)Gold,
                    DrawRewardCount = 3,
                    MonstersKilled = 2,
                    RoomsExplored = 1,
                    NpcsLeft = 2,
                    Perfect = [false, true, true],
                    ClearType = TimeSpaceClearType.FirstClear,
                    Unknown = 1
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedRunShowsTheFailedScore()
        {
            await new Spec("A failed run shows only its end state and no cleared score")
                .GivenAsync(ATimeSpaceThatFails)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheFailedScoreShouldBe, EndStateType.Fail)
                .And(NoClearedScoreShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AClearAwardsTheBoostedExperienceAfterTheScore()
        {
            await new Spec("A level 10 character clearing a minimum level 10 time-space with 2 kills sees 60 and 10 and gains them with its low-level boost")
                .GivenAsync(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldShow_Experience_AndJobExperience, 60, 10)
                .And(TheCharacterShouldGain_Experience_AndJobExperienceAfterTheScore, 120L, 30L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExperienceBuffAndTheRoomRateRaiseOnlyTheGain()
        {
            await new Spec("A doubled room rate and a +100% experience buff quadruple the gain but not the score")
                .GivenAsync(TheCharacterHasAnExperienceBuff)
                .AndAsync(ATimeSpaceThatSucceeds)
                .And(TheRoomRateIs_, 2)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldShow_Experience_AndJobExperience, 60, 10)
                .And(TheCharacterShouldGain_Experience_AndJobExperienceAfterTheScore, 480L, 120L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARewardThatFailsForOnePlayerStillRewardsTheOthers()
        {
            await new Spec("When the first player's experience cannot be awarded, the second still gets the score, the experience and the clear")
                .Given(TheFirstPlayersExperienceCannotBeAwarded)
                .AndAsync(ATimeSpaceThatSucceeds)
                .AndAsync(ASecondPlayerHasEntered)
                .WhenAsync(TheExitIsReached)
                .Then(TheSecondPlayerShouldBeRewarded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExperienceAwardThatFailsHalfwayStillTakesTheStoneAndRecordsTheClear()
        {
            await new Spec("When the award throws after adding the experience, the stone is still taken, the clear recorded and the experience added once")
                .Given(TheExperienceAwardFailsAfterAddingIt)
                .AndAsync(ATimeSpaceOpenedWithAStoneThatSucceeds)
                .WhenAsync(TheExitIsReached)
                .Then(TheStoneShouldBeTaken)
                .And(TheClearShouldBeRecordedOnce)
                .And(TheExperienceShouldBeAddedOnce)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKillAwardedWhileTheClearIsRewardedKeepsBothExperiences()
        {
            await ATimeSpaceThatSucceeds();
            await _MonstersAreKilled(2);
            var before = _session.Character.LevelXp;
            long whileHeld = -1;
            var exit = Task.CompletedTask;

            await _room.Executor.RunAsync(async () =>
            {
                using (System.Threading.ExecutionContext.SuppressFlow())
                {
                    exit = Task.Run(TheExitIsReached);
                }

                await Task.Delay(300);
                whileHeld = _session.Character.LevelXp;
                _session.Character.LevelXp += 100;
            });
            await exit.WaitAsync(TimeSpan.FromSeconds(5));
            for (var i = 0; i < 100 && _session.Character.LevelXp == before + 100; i++)
            {
                await Task.Delay(50);
            }

            Assert.AreEqual(before, whileHeld);
            Assert.IsTrue(_session.Character.LevelXp > before + 100);
        }

        [TestMethod]
        public async Task AFailedRunAwardsNoExperience()
        {
            await new Spec("A failed run awards no experience")
                .GivenAsync(ATimeSpaceThatFails)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(NoExperienceShouldBeGained)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALevelTwentyCharacterScoresOneInTheLastField()
        {
            await new Spec("A character of level 20 or below gets 1 in the score's last field")
                .Given(TheCharacterIsLevel_, (byte)20)
                .AndAsync(ATimeSpaceThatSucceeds)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoresLastFieldShouldBe_, (byte)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHighLevelCharacterScoresFifteenInTheLastField()
        {
            await new Spec("A character of level 76 gets 15 in the score's last field")
                .Given(TheCharacterIsLevel_, (byte)76)
                .AndAsync(ATimeSpaceThatSucceeds)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoresLastFieldShouldBe_, (byte)15)
                .ExecuteAsync();
        }

        private async Task TheCharacterHasAnExperienceBuff()
        {
            var buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(buffs);
            await buffs.ApplyAsync(_session.Character, new CardDto { CardId = 684, Duration = 30000 },
                [new BCardDto { Type = BCardType.CardType.StanceAndExperience, SubType = 41, FirstData = 100, CardId = 684 }],
                _session.Character);
        }

        private void TheRoomRateIs_(int rate) => _room.XpRate = rate;

        private void TheFirstPlayersExperienceCannotBeAwarded()
        {
            var firstId = _session.Character.CharacterId;
            _progression = World.Freeze<Mock<IExperienceProgressionService>>();
            _progression.Setup(p => p.AddExperienceAsync(It.Is<PlayerComponentBundle>(c => c.CharacterId == firstId),
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException());
        }

        private void TheExperienceAwardFailsAfterAddingIt()
        {
            _progression = World.Freeze<Mock<IExperienceProgressionService>>();
            _progression.Setup(p => p.AddExperienceAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<long>(), It.IsAny<long>(),
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()))
                .Callback((PlayerComponentBundle player, long experience, long _, long _, long _, int _) =>
                {
                    _awarded += experience;
                    player.LevelXp += experience;
                })
                .ThrowsAsync(new InvalidOperationException());
        }

        private void TheStoneShouldBeTaken() => Assert.AreEqual(0, _session.Character.InventoryService.CountItem(Stone));

        private void TheClearShouldBeRecordedOnce() =>
            _gameEvents.Verify(e => e.RaiseAsync(It.IsAny<ScriptedInstanceSucceededEvent>()), Times.Once);

        private void TheExperienceShouldBeAddedOnce()
        {
            _progression.Verify(p => p.AddExperienceAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<long>(), It.IsAny<long>(),
                It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()), Times.Once);
            Assert.AreEqual(_awarded, _session.Character.LevelXp);
        }

        private async Task ASecondPlayerHasEntered()
        {
            _second = await World.CreateSessionAsync();
            _second.Character.Level = LevelMinimum;
            GameObject.Services.MapChangeService.MapChangeService.MovePlayer(_second, _second.Character.MapInstance, _room, null, null);
            World.SessionRegistry.UpdateCharacter(_second.Channel?.Id ?? string.Empty, _second.Character.CharacterId, _room.MapInstanceId);
            await _run.EnteredAsync(_second, _room.MapInstanceId);
        }

        private void TheSecondPlayerShouldBeRewarded()
        {
            var secondId = _second.Character.CharacterId;
            Assert.AreEqual(1, _second.LastPackets.OfType<ScorePacket>().Count());
            _progression.Verify(p => p.AddExperienceAsync(It.Is<PlayerComponentBundle>(c => c.CharacterId == secondId),
                It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()), Times.Once);
            _gameEvents.Verify(e => e.RaiseAsync(It.Is<ScriptedInstanceSucceededEvent>(evt => evt.Character.CharacterId == secondId)),
                Times.Once);
        }

        private void TheCharacterIsLevel_(byte level) => _session.Character.Level = level;

        private void TheScoresLastFieldShouldBe_(byte unknown) =>
            Assert.AreEqual(unknown, _session.LastPackets.OfType<ScorePacket>().Single().Unknown);

        private void TheCharacterHasClearedItBefore() =>
            _session.Character.CompletedTimeSpaces = ImmutableHashSet.Create(TimeSpaceId);

        private void TheRunIsInLoserMode() => _run.IsLoserMode = true;

        private void TheClearIsRecordedWhenItSucceeds()
        {
            var handler = World.Create<TimeSpaceSucceededHandler>();
            _gameEvents.Setup(e => e.RaiseAsync(It.IsAny<ScriptedInstanceSucceededEvent>()))
                .Returns((ScriptedInstanceSucceededEvent evt) => handler.Handle(evt));
        }

        private Task ATimeSpaceThatSucceeds() => ATimeSpaceAsync(true);

        private Task ATimeSpaceWithAHunterThatSucceeds() => ATimeSpaceAsync(true, hunts: true);

        private Task ATimeSpaceOpenedWithAStoneThatSucceeds()
        {
            var stone = InventoryItemInstance.Create(new ItemInstance(new Item
            {
                VNum = Stone, Type = NoscorePocketType.Main, ItemType = ItemType.Special
            })
            {
                Amount = 1,
            }, _session.Character.CharacterId);
            stone.Slot = 0;
            stone.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[stone.ItemInstanceId] = stone;
            return ATimeSpaceAsync(true, stone: Stone);
        }

        private Task ATimeSpaceThatFails() => ATimeSpaceAsync(false);

        private async Task ATimeSpaceAsync(bool succeeds, bool hunts = false, short? stone = null)
        {
            _room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            var script = new Script(succeeds, hunts);
            _run = new ScriptedInstanceRun(
                World.CreateTimeSpaceRunHost(new GameObject.Services.ScriptedInstanceService.ScriptedInstance
                    {
                        ScriptedInstanceId = 1, MapId = 1, LevelMinimum = LevelMinimum, Logic = script, StoneVNum = stone
                    }),
                script,
                new Dictionary<int, MapInstance> { [1] = _room },
                new NpcMonsterCatalog(
                [
                    new NpcMonsterDto { NpcMonsterVNum = Grunt, MaxHp = 10, IsHostile = true, Level = LevelMinimum + 1 },
                    new NpcMonsterDto { NpcMonsterVNum = Villager, MaxHp = 10 }
                ]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            GameObject.Services.MapChangeService.MapChangeService.MovePlayer(_session, _session.Character.MapInstance, _room, null, null);
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, _room.MapInstanceId);
            await _run.EnteredAsync(_session, _room.MapInstanceId);
        }

        private async Task _MonstersAreKilled(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await _run.MonsterKilledAsync(_room.MapInstanceId, Grunt, killerLevel: LevelMinimum);
            }
        }

        private Task AnNpcFalls()
        {
            var scriptedInstances = World.Freeze<Mock<IScriptedInstanceService>>();
            scriptedInstances.Setup(s => s.GetRun(_room.MapInstanceId)).Returns(_run);
            return World.Create<TimeSpaceProgressHandler>().Handle(new EntityDiedEvent(_room.EnumerateNpcs().First(), null));
        }

        private Task TheExitIsReached() => _run.ExitReachedAsync(_room.MapInstanceId);

        private void TheScoreShouldBe(ScorePacket expected) =>
            PacketAssert.AreEqual(expected, _session.LastPackets.OfType<ScorePacket>().Single());

        private void TheScoreShouldShow_Experience_AndJobExperience(int experience, int jobExperience)
        {
            var score = _session.LastPackets.OfType<ScorePacket>().Single();
            Assert.AreEqual((experience, jobExperience), (score.Experience, score.JobExperience));
        }

        private void TheCharacterShouldGain_Experience_AndJobExperienceAfterTheScore(long experience, long jobExperience)
        {
            var packets = _session.LastPackets.ToList();
            var lev = packets.OfType<LevPacket>().Single();
            var character = _session.Character;
            PacketAssert.AreEqual(new LevPacket
            {
                Level = LevelMinimum,
                LevelXp = experience,
                JobLevel = JobLevel,
                JobLevelXp = jobExperience,
                XpLoad = World.Create<IExperienceService>().GetExperience(LevelMinimum),
                JobXpLoad = World.Create<IJobExperienceService>().GetJobExperience(character.Class, JobLevel),
                Reputation = character.Reputation,
                SkillCp = character.RemainingSkillPoints(World.Create<ISkillPointService>())
            }, lev);
            Assert.IsTrue(packets.IndexOf(packets.OfType<ScorePacket>().Single()) < packets.IndexOf(lev));
        }

        private void NoExperienceShouldBeGained()
        {
            Assert.IsFalse(_session.LastPackets.OfType<LevPacket>().Any());
            Assert.AreEqual((0L, 0L), (_session.Character.LevelXp, _session.Character.JobLevelXp));
        }

        private void TheFailedScoreShouldBe(EndStateType state) =>
            PacketAssert.AreEqual(new ScoreFailedPacket { EndState = state },
                _room.LastPackets.OfType<ScoreFailedPacket>().Single());

        private void NoClearedScoreShouldBeSent() =>
            Assert.IsFalse(_room.LastPackets.OfType<ScorePacket>().Any() || _session.LastPackets.OfType<ScorePacket>().Any());

        private void TheClearShouldBeRecorded() =>
            Assert.IsTrue(_session.Character.CompletedTimeSpaces?.Contains(TimeSpaceId));
    }
}

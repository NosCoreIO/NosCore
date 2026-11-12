//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class StatDataMessageHandlerTests
    {
        private TestWorld World;
        private StatDataMessageChannelCommunicationMessageHandler Handler;
        private ClientSession Session;
        private Mock<ISessionRegistry> SessionRegistry;
        private Mock<ILogger<StatDataMessageChannelCommunicationMessageHandler>> Logger;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> LogLanguage;
        private IOptions<WorldConfiguration> WorldConfig;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            SessionRegistry = World.Freeze<Mock<ISessionRegistry>>();
            Logger = World.Freeze<Mock<ILogger<StatDataMessageChannelCommunicationMessageHandler>>>();
            LogLanguage = World.Freeze<Mock<ILogLanguageLocalizer<LogLanguageKey>>>();
            WorldConfig = Options.Create(new WorldConfiguration { Database = new(), MasterCommunication = new(), MaxGoldAmount = 999999999 });
            World.Inject(WorldConfig);
            Handler = World.Create<StatDataMessageChannelCommunicationMessageHandler>();
        }

        [TestMethod]
        public async Task HandleShouldUpdateLevelWhenActionTypeIsUpdateLevel()
        {
            await new Spec("Handle should update level when action type is update level")
                .Given(CharacterIsRegistered)
                .And(OriginalLevelIs_, 1)
                .WhenAsync(HandlingStatDataWithLevel_, 50)
                .Then(CharacterLevelShouldBe_, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldDoNothingWhenCharacterNotFound()
        {
            await new Spec("Handle should do nothing when character not found")
                .Given(CharacterIsNotRegistered)
                .And(OriginalLevelIs_, 1)
                .WhenAsync(HandlingStatDataWithLevel_, 50)
                .Then(CharacterLevelShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldLogErrorForUnknownActionType()
        {
            await new Spec("Handle should log error for unknown action type")
                .Given(CharacterIsRegistered)
                .WhenAsync(HandlingStatDataWithUnknownActionType)
                .Then(ShouldLogError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARemoteClassChangeResetsTheSkillBarLikeALocalOne()
        {
            await new Spec("A remote class change resets the skill bar like a local one")
                .Given(CharacterIsRegistered)
                .And(CharacterKnowsAnAdventurerSkill)
                .WhenAsync(HandlingStatDataWithClass_, CharacterClassType.Swordsman)
                .Then(CharacterClassShouldBe_, CharacterClassType.Swordsman)
                .And(OldSkillsShouldBeGone)
                .ExecuteAsync();
        }

        private void CharacterKnowsAnAdventurerSkill() =>
            Session.Character.Skills.TryAdd(200, new NosCore.GameObject.Services.BattleService.CharacterSkill
            {
                SkillVNum = 200,
                CharacterId = Session.Character.CharacterId,
                Skill = new SkillDto { SkillVNum = 200 }
            });

        private async Task HandlingStatDataWithClass_(CharacterClassType classType)
        {
            await Handler.Handle(new StatData
            {
                Character = new Data.WebApi.Character { Name = Session.Character.Name },
                ActionType = UpdateStatActionType.UpdateClass,
                Data = (long)classType
            });
        }

        private void CharacterClassShouldBe_(CharacterClassType classType) =>
            Assert.AreEqual(classType, Session.Character.Class);

        private void OldSkillsShouldBeGone() =>
            Assert.IsFalse(Session.Character.Skills.ContainsKey(200));

        private byte OriginalLevel;

        private void CharacterIsRegistered()
        {
            SessionRegistry.Setup(x => x.GetSession(It.IsAny<System.Func<ClientSession, bool>>()))
                .Returns(Session);
        }

        private void CharacterIsNotRegistered()
        {
            SessionRegistry.Setup(x => x.GetSession(It.IsAny<System.Func<ClientSession, bool>>()))
                .Returns((ClientSession?)null);
        }

        private void OriginalLevelIs_(int level)
        {
            OriginalLevel = (byte)level;
            Session.Character.Level = OriginalLevel;
        }

        private async Task HandlingStatDataWithLevel_(int newLevel)
        {
            await Handler.Handle(new StatData
            {
                Character = new Data.WebApi.Character { Name = Session.Character.Name },
                ActionType = UpdateStatActionType.UpdateLevel,
                Data = newLevel
            });
        }

        private async Task HandlingStatDataWithUnknownActionType()
        {
            await Handler.Handle(new StatData
            {
                Character = new Data.WebApi.Character { Name = Session.Character.Name },
                ActionType = unchecked((UpdateStatActionType)999),
                Data = 1
            });
        }

        private void CharacterLevelShouldBe_(int expectedLevel)
        {
            Assert.AreEqual((byte)expectedLevel, Session.Character.Level);
        }

        private void ShouldLogError()
        {
            Logger.Verify(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => true),
                    It.IsAny<System.Exception?>(),
                    It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
                Times.Once);
        }
    }
}

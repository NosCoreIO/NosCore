//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class StatDataMessageHandlerTests
    {
        private StatDataMessageChannelCommunicationMessageHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<ISessionRegistry> SessionRegistry = null!;
        private Mock<ILogger> Logger = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> LogLanguage = null!;
        private IOptions<WorldConfiguration> WorldConfig = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SessionRegistry = new Mock<ISessionRegistry>();
            Logger = new Mock<ILogger>();
            LogLanguage = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            WorldConfig = Options.Create(new WorldConfiguration { MaxGoldAmount = 999999999 });
            Handler = new StatDataMessageChannelCommunicationMessageHandler(Logger.Object, LogLanguage.Object, WorldConfig, SessionRegistry.Object);
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

        private byte OriginalLevel;

        private void CharacterIsRegistered()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns(Session.Character);
        }

        private void CharacterIsNotRegistered()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns((ICharacterEntity?)null);
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
            Logger.Verify(x => x.Error(It.IsAny<string>()), Times.Once);
        }
    }
}

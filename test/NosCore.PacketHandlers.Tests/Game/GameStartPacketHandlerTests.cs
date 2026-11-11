//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.SkillService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Shared.Enumerations;
using System.Linq;
using SpecLight;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GameStartPacketHandlerTests
    {
        private GameStartPacketHandler Handler;
        private ClientSession Session;
        private Mock<IMapChangeService> MapChangeService;
        private Mock<ISkillService> SkillService;
        private Mock<IQuestService> QuestService;
        private readonly Mock<ILogger<GameStartPacketHandler>> Logger = new();
        private TaskCompletionSource ScriptRun;
        private Task GameStart;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            var friendHub = new Mock<IFriendHub>();
            var channelHub = new Mock<IChannelHub>();
            var pubSubHub = new Mock<IPubSubHub>();
            var blacklistHub = new Mock<IBlacklistHub>();
            var mailHub = new Mock<IMailHub>();
            QuestService = new Mock<IQuestService>();
            MapChangeService = new Mock<IMapChangeService>();
            SkillService = new Mock<ISkillService>();
            var serializer = new Mock<ISerializer>();

            friendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            blacklistHub.Setup(s => s.GetBlacklistedAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            mailHub.Setup(s => s.GetMails(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<MailData>());

            Handler = new GameStartPacketHandler(
                TestHelpers.Instance.WorldConfiguration,
                friendHub.Object,
                channelHub.Object,
                pubSubHub.Object,
                blacklistHub.Object,
                serializer.Object,
                mailHub.Object,
                QuestService.Object,
                MapChangeService.Object,
                SkillService.Object,
                new NosCore.Algorithm.FamilyExperienceService.FamilyExperienceService(),
                Logger.Object);
        }

        [TestMethod]
        public async Task GameStartShouldWaitForTheFirstQuestScript()
        {
            await new Spec("Game start should wait for the first quest script")
                .Given(CharacterIsReadyToStart)
                .And(CharacterHasNoScriptYet)
                .And(TheQuestScriptIsStillRunning)
                .When(StartingTheGame)
                .Then(GameStartShouldNotHaveFinished)
                .AndAsync(TheQuestScriptFinishesAsync)
                .AndAsync(GameStartShouldFinishAsync)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartShouldLogAFailedQuestScriptAndCarryOn()
        {
            await new Spec("Game start should log a failed quest script and carry on")
                .Given(CharacterIsReadyToStart)
                .And(CharacterHasNoScriptYet)
                .And(TheQuestScriptFails)
                .WhenAsync(ExecutingGameStart)
                .Then(GameStartedShouldBeTrue)
                .And(AnErrorShouldBeLogged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWhenAlreadyStartedShouldDoNothing()
        {
            await new Spec("Game start when already started should do nothing")
                .Given(GameAlreadyStarted)
                .WhenAsync(ExecutingGameStart)
                .Then(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithoutSelectedCharacterShouldDoNothing()
        {
            await new Spec("Game start without selected character should do nothing")
                .Given(NoCharacterSelected)
                .WhenAsync(ExecutingGameStart)
                .Then(GameStartedShouldBeFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithValidCharacterShouldSetGameStartedFlag()
        {
            await new Spec("Game start with valid character should set game started flag")
                .Given(CharacterIsReadyToStart)
                .WhenAsync(ExecutingGameStart)
                .Then(GameStartedShouldBeTrue)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithPositiveHpShouldCallMapChange()
        {
            await new Spec("Game start with positive HP should call map change")
                .Given(CharacterIsReadyToStart)
                .And(CharacterHasPositiveHp)
                .WhenAsync(ExecutingGameStart)
                .Then(MapChangeShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartShouldLoadSkills()
        {
            await new Spec("Game start should load skills")
                .Given(CharacterIsReadyToStart)
                .WhenAsync(ExecutingGameStart)
                .Then(SkillServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithZeroHpShouldNotCallMapChange()
        {
            await new Spec("Game start with zero HP should not call map change")
                .Given(CharacterIsReadyToStart)
                .And(CharacterHasZeroHp)
                .WhenAsync(ExecutingGameStart)
                .Then(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        private void CharacterHasNoScriptYet() => Session.Character.Script = null;

        private void TheQuestScriptIsStillRunning()
        {
            ScriptRun = new TaskCompletionSource();
            QuestService.Setup(s => s.RunScriptAsync(It.IsAny<ICharacterEntity>())).Returns(ScriptRun.Task);
        }

        private void TheQuestScriptFails() =>
            QuestService.Setup(s => s.RunScriptAsync(It.IsAny<ICharacterEntity>()))
                .ThrowsAsync(new InvalidOperationException("script data missing"));

        private void StartingTheGame() => GameStart = Handler.ExecuteAsync(new GameStartPacket(), Session);

        private void GameStartShouldNotHaveFinished() => Assert.IsFalse(GameStart.IsCompleted);

        private Task TheQuestScriptFinishesAsync()
        {
            ScriptRun.SetResult();
            return Task.CompletedTask;
        }

        private async Task GameStartShouldFinishAsync()
        {
            await GameStart.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(Session.GameStarted);
        }

        private void AnErrorShouldBeLogged() =>
            Logger.Verify(o => o.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        private void GameAlreadyStarted()
        {
            Session.GameStarted = true;
        }

        private void NoCharacterSelected()
        {
            Session.GameStarted = false;
            Session.HasSelectedCharacter = false;
        }

        private void CharacterIsReadyToStart()
        {
            Session.GameStarted = false;
            Session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();
        }

        private void CharacterHasPositiveHp()
        {
            Session.Character.Hp = 100;
        }

        private void CharacterHasZeroHp()
        {
            Session.Character.Hp = 0;
        }

        private async Task ExecutingGameStart()
        {
            await Handler.ExecuteAsync(new GameStartPacket(), Session);
        }

        [TestMethod]
        public async Task GameStartListsThePetInThePartyPanel()
        {
            await new Spec("The login pinit carries the pet that is out")
                .Given(CharacterIsReadyToStart)
                .And(CharacterHasAPetOut)
                .WhenAsync(ExecutingGameStart)
                .Then(ThePartyPanelShouldListThePet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartLetsAFamilyMemberAskForTheHistory()
        {
            await new Spec("A family member logging in is told to ask for the family history")
                .Given(CharacterIsReadyToStart)
                .And(CharacterIsInAFamily)
                .WhenAsync(ExecutingGameStart)
                .Then(TheHistoryRefreshShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithoutAFamilySendsNoHistoryRefresh()
        {
            await new Spec("A character without a family is not told to ask for a history")
                .Given(CharacterIsReadyToStart)
                .WhenAsync(ExecutingGameStart)
                .Then(NoHistoryRefreshShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsInAFamily() => Session.Character.Family = new NosCore.GameObject.Services.FamilyService.Family
        {
            FamilyId = 1,
            Name = "Testers",
            Members = new List<FamilyCharacterDto>
            {
                new() { FamilyId = 1, CharacterId = Session.Character.CharacterId, Authority = FamilyAuthority.Head }
            }
        };

        private void TheHistoryRefreshShouldBeSent() => Assert.IsTrue(Session.LastPackets.Any(p => p is FhisStcPacket));

        private void NoHistoryRefreshShouldBeSent() => Assert.IsFalse(Session.LastPackets.Any(p => p is FhisStcPacket));

        private NosCore.GameObject.Services.MateService.Mate _pet;

        private void CharacterHasAPetOut()
        {
            _pet = TestMates.Pet(Session.Character.CharacterId);
            Session.Character.Mates[_pet.MateTransportId] = _pet;
        }

        private void ThePartyPanelShouldListThePet()
        {
            var pinit = Session.LastPackets.OfType<PinitPacket>().Last();
            Assert.IsTrue(pinit.PinitSubPackets.NotNull().Any(s => s.NotNull().VisualType == VisualType.Npc && s.NotNull().VisualId == _pet.MateTransportId));
        }

        private void GameStartedShouldBeFalse()
        {
            Assert.IsFalse(Session.GameStarted);
        }

        private void GameStartedShouldBeTrue()
        {
            Assert.IsTrue(Session.GameStarted);
        }

        private void MapChangeShouldNotBeCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(It.IsAny<ClientSession>()), Times.Never);
        }

        private void MapChangeShouldBeCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(It.IsAny<ClientSession>()), Times.Once);
        }

        private void SkillServiceShouldBeCalled()
        {
            SkillService.Verify(x => x.LoadSkill(It.IsAny<ICharacterEntity>()), Times.Once);
        }
    }
}

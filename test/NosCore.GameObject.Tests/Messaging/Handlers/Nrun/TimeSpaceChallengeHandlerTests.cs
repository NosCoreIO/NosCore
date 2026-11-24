//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class TimeSpaceChallengeHandlerTests
    {
        private const short TimeSpace = 518;

        private TestWorld World;
        private ClientSession Session;
        private Mock<ITimeSpaceEntryService> Entry;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Entry = World.Freeze<Mock<ITimeSpaceEntryService>>();
        }

        private CharacterQuest HoldingTheQuestFor_(short timeSpaceId)
        {
            var quest = new CharacterQuest
            {
                Id = Guid.NewGuid(),
                QuestId = 2006,
                Quest = new GameObject.Services.QuestService.Quest
                {
                    QuestId = 2006,
                    QuestType = QuestType.TimesSpace,
                    QuestObjectives = [new QuestObjectiveDto { QuestObjectiveId = Guid.NewGuid(), QuestId = 2006, FirstData = timeSpaceId, SecondData = 1 }],
                },
            };
            Session.Character.Quests.TryAdd(quest.Id, quest);
            return quest;
        }

        private Task PressingTimeSpaceChallengeFor_(short timeSpaceId) => World.Create<TimeSpaceChallengeHandler>()
            .HandleAsync(Session, Session.Character, new NrunPacket { Runner = NrunRunnerType.GoToXTimeSpace, Type = timeSpaceId });

        private void NoTimeSpaceShouldBeEntered() =>
            Entry.Verify(e => e.EnterAsync(It.IsAny<ClientSession>(), It.IsAny<ScriptedInstance>(), It.IsAny<bool>()), Times.Never);

        [TestMethod]
        [DataRow((short)518)]
        [DataRow((short)519)]
        [DataRow((short)520)]
        [DataRow((short)521)]
        public async Task TheChallengeEntersTheTimeSpaceTheQuestAsksFor(short timeSpaceId)
        {
            HoldingTheQuestFor_(timeSpaceId);

            await PressingTimeSpaceChallengeFor_(timeSpaceId);

            var (mapId, x, y) = (Session.Character.MapId, Session.Character.PositionX, Session.Character.PositionY);
            var script = $"timespace-{timeSpaceId}";
            Entry.Verify(e => e.EnterAsync(Session, It.Is<ScriptedInstance>(i => i.Script == script
                && i.Type == ScriptedInstanceType.TimeSpace && i.MapId == mapId && i.PositionX == x && i.PositionY == y), false), Times.Once);
        }

        [TestMethod]
        public async Task WithoutTheQuestNoTimeSpaceIsEntered()
        {
            await PressingTimeSpaceChallengeFor_(TimeSpace);

            NoTimeSpaceShouldBeEntered();
        }

        [TestMethod]
        public async Task AnotherTimeSpaceThanTheQuestsIsNotEntered()
        {
            HoldingTheQuestFor_(TimeSpace);

            await PressingTimeSpaceChallengeFor_(519);

            NoTimeSpaceShouldBeEntered();
        }

        [TestMethod]
        public async Task AQuestAlreadyMetEntersNoTimeSpace()
        {
            HoldingTheQuestFor_(TimeSpace).ObjectivesCompletedOn = Instant.FromUnixTimeSeconds(1);

            await PressingTimeSpaceChallengeFor_(TimeSpace);

            NoTimeSpaceShouldBeEntered();
        }
    }
}

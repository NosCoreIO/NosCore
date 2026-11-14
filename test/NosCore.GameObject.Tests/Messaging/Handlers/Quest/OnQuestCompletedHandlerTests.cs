//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Quest
{
    [TestClass]
    public class OnQuestCompletedHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();
            World.Freeze<Mock<IQuestService>>();
        }

        private CharacterQuest Done(short questId, bool isDaily = false, int hoursAgo = 0)
        {
            var quest = new CharacterQuest
            {
                QuestId = questId,
                CompletedOn = World.Clock.GetCurrentInstant() - NodaTime.Duration.FromHours(hoursAgo),
                Quest = new GameObject.Services.QuestService.Quest { QuestId = questId, IsDaily = isDaily, QuestObjectives = [] }
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), quest);
            return quest;
        }

        [TestMethod]
        public async Task ACompletedQuestIsMarkedDoneInItsBlock()
        {
            Done(6040);
            var quest = Done(6041);

            await World.Create<OnQuestCompletedHandler>().Handle(new QuestCompletedEvent(Session.Character, quest));

            var block = Session.LastPackets.OfType<SqstPacket>().Single();
            Assert.AreEqual(4, block.Type);
            Assert.AreEqual(SqstPacket.FromCompletedQuests(4, [6040, 6041]).Bitmap, block.Bitmap);
        }

        [TestMethod]
        public async Task TheQuestWindowDropsACompletedQuest()
        {
            var quest = Done(6041);

            await World.Create<OnQuestCompletedHandler>().Handle(new QuestCompletedEvent(Session.Character, quest));

            Assert.AreEqual(0, Session.LastPackets.OfType<QstlistPacket>().Single().QuestSubPackets.Count);
        }

        [TestMethod]
        public async Task ADailyQuestDoneTodayIsMarkedDone()
        {
            var quest = Done(6041, isDaily: true);

            await World.Create<OnQuestCompletedHandler>().Handle(new QuestCompletedEvent(Session.Character, quest));

            Assert.AreEqual(SqstPacket.FromCompletedQuests(4, [6041]).Bitmap, Session.LastPackets.OfType<SqstPacket>().Single().Bitmap);
        }

        [TestMethod]
        public async Task ADailyQuestDoneMoreThanADayAgoCanBeTakenAgain()
        {
            Done(6040, isDaily: true, hoursAgo: 25);
            var quest = Done(6041);

            await World.Create<OnQuestCompletedHandler>().Handle(new QuestCompletedEvent(Session.Character, quest));

            Assert.AreEqual(SqstPacket.FromCompletedQuests(4, [6041]).Bitmap, Session.LastPackets.OfType<SqstPacket>().Single().Bitmap);
        }
    }
}

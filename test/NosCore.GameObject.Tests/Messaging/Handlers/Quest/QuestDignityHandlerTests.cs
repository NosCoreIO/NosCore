//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Quest
{
    [TestClass]
    public class QuestDignityHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task CompletingAQuestRestoresDignityToTheMaximum()
        {
            await new Spec("Completing a quest puts dignity back at the maximum, however low it was")
                .Given(TheCharacterHas_Dignity, (short)-72)
                .WhenAsync(AQuestIsCompleted)
                .Then(TheCharacterShouldHave_Dignity, (short)100)
                .And(TheBarShouldShow_Dignity, (short)100)
                .And(NothingShouldBeAnnounced)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AtTheMaximumTheBarIsStillSent()
        {
            await new Spec("A quest completed at the maximum leaves dignity there and still refreshes the bar")
                .Given(TheCharacterHas_Dignity, (short)100)
                .WhenAsync(AQuestIsCompleted)
                .Then(TheCharacterShouldHave_Dignity, (short)100)
                .And(TheBarShouldShow_Dignity, (short)100)
                .ExecuteAsync();
        }

        private void TheCharacterHas_Dignity(short dignity) => _session.Character.Dignity = dignity;

        private Task AQuestIsCompleted() => World.Create<QuestDignityHandler>().Handle(new QuestCompletedEvent(_session.Character,
            new CharacterQuest { QuestId = 1527, Quest = new GameObject.Services.QuestService.Quest { QuestId = 1527, QuestObjectives = [] } }));

        private void TheCharacterShouldHave_Dignity(short dignity) => Assert.AreEqual(dignity, _session.Character.Dignity);

        private void TheBarShouldShow_Dignity(short dignity) =>
            Assert.AreEqual(dignity, _session.LastPackets.OfType<FdPacket>().Single().Dignity);

        private void NothingShouldBeAnnounced() => Assert.IsFalse(_session.LastPackets.OfType<SayiPacket>().Any());
    }
}

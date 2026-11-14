//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class ScriptOfferHandlersTests
    {
        private const long NpcId = 7142;
        private TestWorld World;
        private ClientSession Session;
        private Mock<IQuestService> QuestService;
        private readonly List<ScriptOfferDto> Offers =
        [
            new() { ScriptId = 12, NpcVNum = 394, RequiredScriptId = 11, LevelMinimum = 56, DialogId = 52 },
            new() { ScriptId = 13, NpcVNum = 394, RequiredScriptId = 12, LevelMinimum = 57, DialogId = 52 },
        ];

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            QuestService = World.Freeze<Mock<IQuestService>>();
            World.Inject(Offers);
        }

        private static INonPlayableEntity Npc(short vnum)
        {
            var npc = new Mock<INonPlayableEntity>();
            npc.SetupGet(n => n.NpcMonster).Returns(new NpcMonsterDto { NpcMonsterVNum = vnum });
            npc.SetupGet(n => n.VisualType).Returns(VisualType.Npc);
            npc.SetupGet(n => n.VisualId).Returns(NpcId);
            return npc.Object;
        }

        [TestMethod]
        public async Task GetQuestOpensTheNpcsChapterList()
        {
            await World.Create<ScriptOfferListHandler>().HandleAsync(Session, Npc(394),
                new NrunPacket { Runner = NrunRunnerType.GetQuestListFromNPC, Type = 0 });

            var dialog = Session.LastPackets.OfType<RequestNpcPacket>().Single();
            Assert.AreEqual(NpcId, dialog.TargetId);
            Assert.AreEqual(52, dialog.Data);
        }

        [TestMethod]
        public async Task GetQuestDoesNothingAtAnNpcWithoutChapters()
        {
            await World.Create<ScriptOfferListHandler>().HandleAsync(Session, Npc(300),
                new NrunPacket { Runner = NrunRunnerType.GetQuestListFromNPC, Type = 0 });

            Assert.AreEqual(0, Session.LastPackets.OfType<RequestNpcPacket>().Count());
        }

        [TestMethod]
        public async Task PickingAChapterAsksTheQuestServiceToStartIt()
        {
            await World.Create<TakeScriptOfferHandler>().HandleAsync(Session, Npc(394),
                new NrunPacket { Runner = NrunRunnerType.GetQuestFromList, Type = 13 });

            QuestService.Verify(q => q.TakeScriptOfferAsync(It.IsAny<PlayerComponentBundle>(), 394, 13), Times.Once);
        }

        [TestMethod]
        public async Task PickingAChapterWithoutAnNpcDoesNothing()
        {
            await World.Create<TakeScriptOfferHandler>().HandleAsync(Session, null,
                new NrunPacket { Runner = NrunRunnerType.GetQuestFromList, Type = 13 });

            QuestService.Verify(q => q.TakeScriptOfferAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<short>(), It.IsAny<byte>()), Times.Never);
        }
    }
}

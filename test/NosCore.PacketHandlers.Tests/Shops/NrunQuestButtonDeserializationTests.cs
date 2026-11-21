//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.QuestService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.PacketHandlers.Tests.Shops
{
    // A runner the deserializer has no member for makes the whole packet invalid, so it never reaches a handler.
    [TestClass]
    public class NrunQuestButtonDeserializationTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IQuestService> QuestService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            QuestService = World.Freeze<Mock<IQuestService>>();
            World.Inject(new List<QuestDto> { new() { QuestId = 6807, QuestType = QuestType.Dialog1, LevelMin = 80, LevelMax = 99 } });
            World.Inject(new List<NpcTalkButtonDto>
            {
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = 467, Runner = NrunRunnerType.GetQuestTheMerlingsCurse, Argument = 6807 },
            });
            World.UseReal<INpcTalkButtonCatalog, NpcTalkButtonCatalog>();
            Session.WithNpcs(new NosCore.Data.Dto.MapNpcDto { MapNpcId = 2001, MapX = 1, MapY = 1, Dialog = 467 });
        }

        [TestMethod]
        public async Task TheMerlingsCurseButtonReachesItsHandler()
        {
            var serializer = new Serializer(PacketTypes.World());
            var deserializer = new Deserializer(PacketTypes.World());
            var packet = deserializer.Deserialize(serializer.Serialize(new NrunPacket
            {
                Runner = NrunRunnerType.GetQuestTheMerlingsCurse, Type = 6807, VisualType = VisualType.Npc, VisualId = 2001
            }));
            World.Inject<IEnumerable<INrunEventHandler>>([World.Create<MerlingsCurseQuestHandler>()]);
            var handler = World.Create<NrunPacketHandler>();
            await World.Create<RequestNpcPacketHandler>()
                .ExecuteAsync(new RequestNpcPacket { Type = VisualType.Npc, TargetId = 2001 }, Session);

            Assert.IsInstanceOfType<NrunPacket>(packet);
            Assert.IsTrue(((NrunPacket)packet).IsValid);
            await handler.ExecuteAsync((NrunPacket)packet, Session);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), QuestActionType.Achieve, 6807), Times.Once);
        }
    }
}

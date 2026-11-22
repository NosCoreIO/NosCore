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
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class GoToQuestTimeSpaceHandlerTests
    {
        private const short GuideDialog = 13;
        private const short OfferedTimeSpace = 519;

        private TestWorld World;
        private ClientSession Session;
        private Mock<ITimeSpaceEntryService> Entry;
        private NpcComponentBundle Guide;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Entry = World.Freeze<Mock<ITimeSpaceEntryService>>();
            World.Inject(new List<NpcTalkButtonDto>
            {
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = GuideDialog, Runner = NrunRunnerType.GoToXTimeSpace, Argument = OfferedTimeSpace },
            });
            World.UseReal<INpcTalkButtonCatalog, NpcTalkButtonCatalog>();
            var map = World.GetBaseMap(TestWorld.TestMapId);
            Guide = new NpcComponentBundle(map.EcsWorld.CreateNpc(1, new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 10 }, map,
                1, 1, 0, 1, 1, false, false, GuideDialog, 0, 0, null), map.EcsWorld);
        }

        private Task PickAsync(NpcComponentBundle? npc, short timeSpaceId) => World.Create<GoToQuestTimeSpaceHandler>()
            .HandleAsync(Session, npc, new NrunPacket { Runner = NrunRunnerType.GoToXTimeSpace, Type = timeSpaceId });

        [TestMethod]
        public async Task TheButtonEntersItsTimeSpace()
        {
            await PickAsync(Guide, OfferedTimeSpace);

            Entry.Verify(s => s.EnterAsync(Session, It.Is<ScriptedInstance>(e =>
                e.TimeSpaceId == OfferedTimeSpace && e.Script == "timespace-519"
                && e.Type == ScriptedInstanceType.TimeSpace), false), Times.Once);
        }

        [TestMethod]
        public async Task ATimeSpaceTheNpcDoesNotOfferIsRefused()
        {
            await PickAsync(Guide, 520);

            Entry.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task WithoutAnNpcNothingHappens()
        {
            await PickAsync(null, OfferedTimeSpace);

            Entry.VerifyNoOtherCalls();
        }
    }
}

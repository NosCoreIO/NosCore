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
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class GoToSpecialistStoneHandlerTests
    {
        private const short GuideDialog = 12;
        private const short OfferedStone = 2107;
        private const short UnofferedStone = 2108;

        private TestWorld World;
        private ClientSession Session;
        private Mock<IMapChangeService> MapChange;
        private NpcComponentBundle Guide;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            MapChange = World.Freeze<Mock<IMapChangeService>>();
            World.Inject(new List<ScriptedInstanceDto>
            {
                new() { ScriptedInstanceId = 1, MapId = 2537, PositionX = 40, PositionY = 25, Type = ScriptedInstanceType.Raid },
                new() { ScriptedInstanceId = 2, MapId = 2536, PositionX = 33, PositionY = 18, Type = ScriptedInstanceType.TimeSpace, TimeSpaceId = OfferedStone },
                new() { ScriptedInstanceId = 3, MapId = 2538, PositionX = 12, PositionY = 9, Type = ScriptedInstanceType.TimeSpace, TimeSpaceId = UnofferedStone },
            });
            World.Inject(new List<NpcTalkButtonDto>
            {
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = GuideDialog, Runner = NrunRunnerType.GoToSPStone, Argument = OfferedStone },
            });
            World.UseReal<INpcTalkButtonCatalog, NpcTalkButtonCatalog>();
            var map = World.GetBaseMap(TestWorld.TestMapId);
            Guide = new NpcComponentBundle(map.EcsWorld.CreateNpc(1, new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 10 }, map,
                1, 1, 0, 1, 1, false, false, GuideDialog, 0, 0, null), map.EcsWorld);
        }

        private Task PickAsync(NpcComponentBundle? npc, short timeSpaceId) => World.Create<GoToSpecialistStoneHandler>()
            .HandleAsync(Session, npc, new NrunPacket { Runner = NrunRunnerType.GoToSPStone, Type = timeSpaceId });

        [TestMethod]
        public async Task TheButtonTakesThePlayerToTheStone()
        {
            await PickAsync(Guide, OfferedStone);

            MapChange.Verify(s => s.ChangeMapAsync(Session, (short)2536, (short)33, (short)18), Times.Once);
        }

        [TestMethod]
        public async Task AStoneTheNpcDoesNotOfferIsRefused()
        {
            await PickAsync(Guide, UnofferedStone);

            MapChange.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task AnUnknownStoneIsIgnored()
        {
            await PickAsync(Guide, 9999);

            MapChange.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task WithoutAnNpcNothingHappens()
        {
            await PickAsync(null, OfferedStone);

            MapChange.VerifyNoOtherCalls();
        }
    }
}

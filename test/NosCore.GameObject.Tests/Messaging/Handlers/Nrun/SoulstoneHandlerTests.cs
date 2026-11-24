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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class SoulstoneHandlerTests
    {
        private const short SoulstoneDialog = 6346;

        private TestWorld World;
        private ClientSession Session;
        private Mock<IMapChangeService> MapChange;
        private NpcComponentBundle Soulstone;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            MapChange = World.Freeze<Mock<IMapChangeService>>();
            World.Inject(new List<NpcTalkButtonDto>
            {
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = SoulstoneDialog, Runner = NrunRunnerType.GoToSPStone, Argument = TestWorld.ShopMapId },
            });
            World.UseReal<INpcTalkButtonCatalog, NpcTalkButtonCatalog>();
            var map = World.GetBaseMap(TestWorld.TestMapId);
            Soulstone = new NpcComponentBundle(map.EcsWorld.CreateNpc(1, new NpcMonsterDto { NpcMonsterVNum = 935, MaxHp = 10 }, map,
                1, 1, 0, 1, 1, false, false, SoulstoneDialog, 0, 0, null), map.EcsWorld);
        }

        private Task TouchingTheSoulstoneFor_(short mapId) => World.Create<SoulstoneHandler>()
            .HandleAsync(Session, Soulstone, new NrunPacket { Runner = NrunRunnerType.GoToSPStone, Type = mapId });

        [TestMethod]
        public async Task TouchingTheSoulstoneTakesThePlayerIntoItsRoom()
        {
            await TouchingTheSoulstoneFor_(TestWorld.ShopMapId);

            MapChange.Verify(m => m.ChangeMapAsync(Session, TestWorld.ShopMapId, It.IsAny<short?>(), It.IsAny<short?>()), Times.Once);
        }

        [TestMethod]
        public async Task ARoomTheSoulstoneDoesNotOfferIsNotEntered()
        {
            await TouchingTheSoulstoneFor_(TestWorld.MinilandMapId);

            MapChange.Verify(m => m.ChangeMapAsync(It.IsAny<ClientSession>(), It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()),
                Times.Never);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class PlayerRevivalHandlerTests
    {
        [TestMethod]
        public async Task NonPlayerVictimIsIgnored()
        {
            // Monster deaths are handled by MonsterRespawnHandler — the revival
            // handler must opt out silently so both handlers can coexist behind
            // the same EntityDiedEvent.
            var registry = new Mock<ISessionRegistry>();
            var mapChange = new Mock<IMapChangeService>();
            var handler = new PlayerRevivalHandler(registry.Object, mapChange.Object, new List<RespawnMapTypeDto>(), new Mock<ILogger>().Object);

            var victim = new Mock<IAliveEntity>();
            victim.SetupGet(v => v.VisualType).Returns(VisualType.Monster);

            await handler.Handle(new EntityDiedEvent(victim.Object, null));

            mapChange.Verify(m => m.ChangeMapAsync(It.IsAny<NosCore.GameObject.Networking.ClientSession.ClientSession>(),
                It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);
        }

        [TestMethod]
        public async Task PlayerWithoutSessionIsIgnored()
        {
            // Disconnected mid-death — we can't warp them, so bail quietly and let
            // their next login flow handle state recovery.
            var registry = new Mock<ISessionRegistry>();
            registry.Setup(r => r.GetSessionByCharacterId(It.IsAny<long>()))
                .Returns((NosCore.GameObject.Networking.ClientSession.ClientSession?)null);
            var mapChange = new Mock<IMapChangeService>();
            var handler = new PlayerRevivalHandler(registry.Object, mapChange.Object, new List<RespawnMapTypeDto>(), new Mock<ILogger>().Object);

            var victim = new Mock<ICharacterEntity>();
            victim.SetupGet(v => v.VisualType).Returns(VisualType.Player);
            victim.SetupGet(v => v.CharacterId).Returns(42);

            await handler.Handle(new EntityDiedEvent(victim.Object, null));

            mapChange.Verify(m => m.ChangeMapAsync(It.IsAny<NosCore.GameObject.Networking.ClientSession.ClientSession>(),
                It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);
        }
    }
}

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
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class DelayBarHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<ISessionGroup> _onlookers;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(0);
            _onlookers = new Mock<ISessionGroup>();
            _session.Character.MapInstance.Sessions = _onlookers.Object;
        }

        private Task StartingABarAsync() =>
            _session.Character.StartDelayAsync(new DelayPacket
            {
                Type = DelayPacketType.Locomotion,
                Delay = 2000,
                Packet = new ClientGuriPacket { Type = GuriPacketType.CollectItem, Argument = 1 }
            }, 1L, World.Clock.GetCurrentInstant());

        private Task TheClientReportsAsync(GuriPacketType type) =>
            World.Create<DelayBarHandler>().Handle(new GuriPacketReceivedEvent(_session, new ClientGuriPacket { Type = type }));

        private IEnumerable<IPacket> Broadcast() =>
            _onlookers.Invocations.SelectMany(i => i.Arguments)
                .SelectMany(a => a is IEnumerable<IPacket> many ? many : a is IPacket one ? new[] { one } : []);

        [TestMethod]
        public async Task TheMapSeesThePlayerBusyOnceTheirBarIsDrawn()
        {
            await StartingABarAsync();

            await TheClientReportsAsync(GuriPacketType.Dance);

            PacketAssert.AreEqual(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Argument = (uint)VisualType.Player,
                EntityId = _session.Character.CharacterId,
                Value = 0
            }, Broadcast().OfType<GuriPacket>().Single());
        }

        [TestMethod]
        public async Task WithoutABarNothingIsShown()
        {
            await TheClientReportsAsync(GuriPacketType.Dance);

            Assert.IsFalse(Broadcast().Any());
            Assert.AreEqual(0, _session.LastPackets.Count);
        }

        [TestMethod]
        public async Task AnInvisiblePlayerIsOnlyShownToThemselves()
        {
            _session.Character.Invisible = true;
            await StartingABarAsync();
            _session.LastPackets.Clear();

            await TheClientReportsAsync(GuriPacketType.Dance);

            Assert.IsFalse(Broadcast().Any());
            Assert.AreEqual(_session.Character.CharacterId, _session.LastPackets.OfType<GuriPacket>().Single().EntityId);
        }

        [TestMethod]
        public async Task TheMapSeesThePartnerBusyOnceItsBarIsDrawn()
        {
            var partner = TestMates.Partner(_session.Character.CharacterId);
            _session.Character.Mates[partner.MateTransportId] = partner;
            await partner.SpawnAsync(_session.Character, _session.Character.MapInstance, 5, 5);
            await _session.Character.StartDelayAsync(new PdelayPacket
            {
                Type = DelayPacketType.Locomotion,
                Delay = 5000,
                Packet = new PslPacket { Type = PslPacketType.Wear }
            }, 1L, World.Clock.GetCurrentInstant());
            _onlookers.Invocations.Clear();

            await TheClientReportsAsync(GuriPacketType.CharacterAnimation);

            PacketAssert.AreEqual(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Argument = (uint)VisualType.Npc,
                EntityId = partner.MateTransportId,
                Value = 0
            }, Broadcast().OfType<GuriPacket>().Single());
        }
    }
}

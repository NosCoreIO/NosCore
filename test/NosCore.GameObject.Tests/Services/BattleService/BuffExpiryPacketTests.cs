//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BuffExpiryPacketTests
    {
        private TestWorld World;
        private static readonly CardDto Card = new() { CardId = 136, Duration = 100 };

        private readonly List<IPacket> _broadcast = new();
        private FakeClock _clock;
        private BuffService _service;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            var mapSessions = new Mock<ISessionGroup>();
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(packets => _broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((packets, _) => _broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            _session.Character.MapInstance.Sessions = mapSessions.Object;
            _clock = new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0));
            _service = new BuffService(_clock, new Mock<ICardCatalog>().Object, new Mock<IRandomProvider>().Object);
        }

        [TestMethod]
        public async Task AnExpiredBuffTakesItsIconDown()
        {
            await _service.ApplyAsync(_session.Character, Card, Array.Empty<BCardDto>(), null);
            _broadcast.Clear();

            _clock.Advance(Duration.FromSeconds(10));
            await _service.TickAsync(_session.Character);

            var bf = _broadcast.OfType<BfPacket>().Single();
            Assert.AreEqual(_session.Character.VisualId, bf.VisualId);
            Assert.AreEqual(Card.CardId, bf.Buff.NotNull().BuffId);
            Assert.AreEqual(0, bf.Buff.NotNull().Duration);
            Assert.IsFalse(_broadcast.OfType<BfePacket>().Any());
        }

        [TestMethod]
        public async Task ABuffStillRunningSendsNothing()
        {
            await _service.ApplyAsync(_session.Character, Card, Array.Empty<BCardDto>(), null);
            _broadcast.Clear();

            _clock.Advance(Duration.FromSeconds(9));
            await _service.TickAsync(_session.Character);

            Assert.AreEqual(0, _broadcast.Count);
        }
    }
}

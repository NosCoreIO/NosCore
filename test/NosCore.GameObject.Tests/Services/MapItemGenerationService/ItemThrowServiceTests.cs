//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MapItemGenerationService
{
    [TestClass]
    public class ItemThrowServiceTests
    {
        private const short Box = 1013;
        private static readonly Spot Boss = new(5, 5);

        private TestWorld World;
        private MapInstance _map;
        private readonly List<IPacket> _seen = [];

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _map = World.GetBaseMap(TestWorld.TestMapId);
            var watchers = new Mock<ISessionGroup>();
            watchers.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(_seen.AddRange).Returns(Task.CompletedTask);
            watchers.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((packets, _) => _seen.AddRange(packets)).Returns(Task.CompletedTask);
            _map.Sessions = watchers.Object;
        }

        [TestMethod]
        public async Task ThrownLootFliesFromTheSpotToWhereItLands()
        {
            await new Spec("Each thrown pile flies from the spot to the cell it lands on, as the item that lies there")
                .Given(TheThrowLands_CellsAway, 2)
                .WhenAsync(Throwing_PilesOf_Async, 2, 3)
                .Then(TwoPilesFlewFromTheSpotTo_, new Spot(7, 7))
                .And(EachThrowIsTheItemOnTheGround)
                .ExecuteAsync();
        }

        private void TheThrowLands_CellsAway(int cells) =>
            World.Random.Setup(s => s.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(cells);

        private Task Throwing_PilesOf_Async(int piles, int amount) =>
            World.Create<ItemThrowService>().ThrowAsync(_map, Boss, [new ItemPile(Box, (short)piles, amount)]);

        private void TwoPilesFlewFromTheSpotTo_(Spot landing)
        {
            var throws = _seen.OfType<ThrowPacket>().ToList();
            Assert.AreEqual(2, throws.Count);
            foreach (var thrown in throws)
            {
                PacketAssert.AreEqual(new ThrowPacket
                {
                    VNum = Box,
                    VisualId = thrown.VisualId,
                    FromX = Boss.X,
                    FromY = Boss.Y,
                    ToX = landing.X,
                    ToY = landing.Y,
                    Amount = 3
                }, thrown);
            }

            Assert.IsFalse(_seen.OfType<DropPacket>().Any());
        }

        private void EachThrowIsTheItemOnTheGround()
        {
            foreach (var thrown in _seen.OfType<ThrowPacket>())
            {
                var item = _map.TryGetMapItem(thrown.VisualId);
                Assert.IsNotNull(item);
                Assert.AreEqual(Box, item.Value.VNum);
                Assert.AreEqual(thrown.ToX, item.Value.PositionX);
                Assert.AreEqual(thrown.ToY, item.Value.PositionY);
            }
        }
    }
}

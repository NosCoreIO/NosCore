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
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class ShoutHerePacketHandlerTests
    {
        private TestWorld World;
        private ShoutHerePacketHandler Handler;
        private ClientSession Session;
        private Mock<ISessionGroup> Onlookers;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(1);
            Onlookers = new Mock<ISessionGroup>();
            Session.Character.MapInstance.Sessions = Onlookers.Object;
            Handler = World.Create<ShoutHerePacketHandler>();
        }

        [TestMethod]
        public async Task TheMessageIsSaidToTheWholeMap()
        {
            await new Spec("The message is said to the whole map")
                .WhenAsync(ShoutingHere_, "Boss in two minutes")
                .Then(TheMapShouldHear_, "Boss in two minutes")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEmptyMessageOnlyShowsTheUsage()
        {
            await new Spec("An empty message only shows the usage to the sender")
                .WhenAsync(ShoutingHere_, " ")
                .Then(TheSenderShouldSeeTheUsage)
                .And(TheMapShouldHearNothing)
                .ExecuteAsync();
        }

        private Task ShoutingHere_(string message) =>
            Handler.ExecuteAsync(new ShoutHerePacket { Message = message }, Session);

        private IEnumerable<IPacket> Broadcast() =>
            Onlookers.Invocations.SelectMany(i => i.Arguments)
                .SelectMany(a => a is IEnumerable<IPacket> many ? many : a is IPacket one ? new[] { one } : []);

        private SayPacket Said(string message) => new()
        {
            VisualType = VisualType.Player,
            VisualId = Session.Character.CharacterId,
            Type = SayColorType.Yellow,
            Message = message,
        };

        private void TheMapShouldHear_(string message) =>
            PacketAssert.AreEqual(Said(message), Broadcast().OfType<SayPacket>().Single());

        private void TheSenderShouldSeeTheUsage() =>
            PacketAssert.AreEqual(Said(new ShoutHerePacket().Help()), Session.LastPackets.OfType<SayPacket>().Single());

        private void TheMapShouldHearNothing() => Assert.IsFalse(Broadcast().Any());
    }
}

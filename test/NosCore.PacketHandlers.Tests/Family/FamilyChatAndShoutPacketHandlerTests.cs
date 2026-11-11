//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Family;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.AutoFixture;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyChatAndShoutPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private readonly List<IMessage> _sent = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.PubSubHub
                .Setup(s => s.SendMessageAsync(It.IsAny<IMessage>()))
                .Callback<IMessage>(_sent.Add)
                .ReturnsAsync(true);
            _session = await World.CreateSessionAsync();
        }

        private void JoinFamily(FamilyAuthority authority, bool managerCanShout = true)
        {
            _session.Character.Family = new GameObject.Services.FamilyService.Family
            {
                FamilyId = 42,
                Name = "Testers",
                ManagerCanShout = managerCanShout,
                Members = new List<FamilyCharacterDto>
                {
                    new() { FamilyId = 42, CharacterId = _session.Character.CharacterId, Authority = authority }
                }
            };
        }

        private Task ChatAsync(string message) =>
            World.Create<FamilyChatPacketHandler>()
                .ExecuteAsync(new FamilyChatPacket { Message = message }, _session);

        private Task ShoutAsync(string message) =>
            World.Create<FamilyShoutPacketHandler>()
                .ExecuteAsync(new FamilyShoutPacket { Message = message }, _session);

        [TestMethod]
        public async Task FamilyChatGoesToTheFamily()
        {
            JoinFamily(FamilyAuthority.Member);

            await ChatAsync(" hello family ");

            var chat = (FamilyChatData)_sent.Single();
            Assert.AreEqual(42, chat.FamilyId);
            Assert.AreEqual(_session.Character.Name, chat.SenderName);
            Assert.AreEqual(_session.Character.MapInstanceId, chat.SenderMapInstanceId);
            Assert.AreEqual("hello family", chat.Message);
        }

        [TestMethod]
        public async Task FamilyChatWithoutAFamilyIsIgnored()
        {
            _session.Character.Family = null;

            await ChatAsync("hello");

            Assert.AreEqual(0, _sent.Count);
        }

        [TestMethod]
        public async Task EmptyFamilyChatIsIgnored()
        {
            JoinFamily(FamilyAuthority.Member);

            await ChatAsync("   ");

            Assert.AreEqual(0, _sent.Count);
        }

        [DataTestMethod]
        [DataRow(FamilyAuthority.Head, true, true)]
        [DataRow(FamilyAuthority.Assistant, false, true)]
        [DataRow(FamilyAuthority.Manager, true, true)]
        [DataRow(FamilyAuthority.Manager, false, false)]
        [DataRow(FamilyAuthority.Member, true, false)]
        public async Task OnlyRanksAllowedToShoutReachTheFamily(FamilyAuthority authority, bool managerCanShout, bool shouts)
        {
            JoinFamily(authority, managerCanShout);

            await ShoutAsync("gather at the farm");

            Assert.AreEqual(shouts, _sent.Count == 1);
            if (shouts)
            {
                var shout = (FamilyShoutData)_sent.Single();
                Assert.AreEqual(42, shout.FamilyId);
                Assert.AreEqual("gather at the farm", shout.Message);
            }
        }

        [TestMethod]
        public async Task ShoutingWithoutAFamilyIsIgnored()
        {
            _session.Character.Family = null;

            await ShoutAsync("hello");

            Assert.AreEqual(0, _sent.Count);
        }
    }
}

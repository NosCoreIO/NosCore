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
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Group;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Group
{
    [TestClass]
    public class GroupInvitePacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private ClientSession TargetSession;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync(new List<GameObject.Infastructure.IPacketHandler> { World.Create<PjoinPacketHandler>() });
            TargetSession = await World.CreateSessionAsync();
            foreach (var session in new[] { Session, TargetSession })
            {
                var groupFactory = new Mock<ISessionGroupFactory>();
                groupFactory.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
                session.Character.Group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, groupFactory.Object);
                session.Character.Group.JoinGroup(session.Character);
            }
        }

        [TestMethod]
        public async Task TypingAConnectedNameInvitesThatPlayer()
        {
            await new Spec("Typing a connected player's name invites them to the party")
                .WhenAsync(Inviting_, (string?)null)
                .Then(TheTargetShouldBeAskedToJoin)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownNameIsAnswered()
        {
            await new Spec("A name nobody connected has is answered with the client's own text")
                .WhenAsync(Inviting_, "Nobody")
                .Then(TheSenderShouldBeToldTheCharacterIsUnknown)
                .And(TheTargetShouldNotBeAsked)
                .ExecuteAsync();
        }

        private Task Inviting_(string? name) =>
            World.Create<GroupInvitePacketHandler>().ExecuteAsync(
                new GroupInvitePacket { CharacterName = name ?? TargetSession.Character.Name }, Session);

        private void TheTargetShouldBeAskedToJoin() =>
            Assert.AreEqual(Game18NConstString.GroupInvite, TargetSession.LastPackets.OfType<Dlgi2Packet>().Single().Question);

        private void TheSenderShouldBeToldTheCharacterIsUnknown() =>
            Assert.AreEqual(Game18NConstString.UnknownCharacter, Session.LastPackets.OfType<InfoiPacket>().Last().Message);

        private void TheTargetShouldNotBeAsked() =>
            Assert.IsFalse(TargetSession.LastPackets.OfType<Dlgi2Packet>().Any());
    }
}

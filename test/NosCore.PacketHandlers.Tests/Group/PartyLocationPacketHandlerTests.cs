//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Group;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Group
{
    [TestClass]
    public class PartyLocationPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private ClientSession MemberSession;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            MemberSession = await World.CreateSessionAsync();
            var groupFactory = new Mock<ISessionGroupFactory>();
            groupFactory.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
            Session.Character.Group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, groupFactory.Object);
            Session.Character.Group.JoinGroup(Session.Character);
        }

        [TestMethod]
        public async Task APlayerWithoutAPartyIsToldSo()
        {
            await new Spec("A player who is not in a party is told so")
                .WhenAsync(AskingWhereThePartyIs_, (string?)null)
                .Then(ThePlayerShouldBeToldTheyAreNotInAParty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryOtherMemberIsLocatedByTheirMap()
        {
            await new Spec("Each other member of the party is named with the map they are on")
                .Given(TheOtherPlayerIsInTheParty)
                .WhenAsync(AskingWhereThePartyIs_, (string?)null)
                .Then(TheMemberShouldBeNamedWithTheirMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANameOutsideThePartyLocatesNobody()
        {
            await new Spec("A name that is not in the party locates nobody")
                .Given(TheOtherPlayerIsInTheParty)
                .WhenAsync(AskingWhereThePartyIs_, "Nobody")
                .Then(NothingShouldBeSaid)
                .ExecuteAsync();
        }

        private void TheOtherPlayerIsInTheParty()
        {
            var group = Session.Character.Group ?? throw new AssertFailedException("The player has no group.");
            group.JoinGroup(MemberSession.Character);
            MemberSession.Character.Group = group;
        }

        private Task AskingWhereThePartyIs_(string? name) =>
            World.Create<PartyLocationPacketHandler>().ExecuteAsync(new PartyLocationPacket { CharacterName = name }, Session);

        private void ThePlayerShouldBeToldTheyAreNotInAParty() =>
            PacketAssert.AreEqual(Session.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotInGroup),
                Session.LastPackets.OfType<SayiPacket>().Single());

        private void TheMemberShouldBeNamedWithTheirMap() =>
            PacketAssert.AreEqual(
                Session.Character.GenerateSayi2(SayColorType.Yellow, Game18NConstString.MapNo, 3,
                    MemberSession.Character.Name, MemberSession.Character.MapInstance.Map.MapId),
                Session.LastPackets.OfType<Sayi2Packet>().Single());

        private void NothingShouldBeSaid() =>
            Assert.IsFalse(Session.LastPackets.OfType<Sayi2Packet>().Any());
    }
}

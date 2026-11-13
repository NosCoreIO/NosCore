//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Group;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Group
{
    [TestClass]
    public class PJoinPacketHandlerTests
    {
        private TestWorld World;
        private static readonly ILogger<PjoinPacketHandler> Logger = new Mock<ILogger<PjoinPacketHandler>>().Object;
        private readonly Dictionary<int, ClientSession> Sessions = new();
        private PjoinPacketHandler PJoinPacketHandler;
        private Mock<IBlacklistHub> BlacklistHub;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var idServer = new IdService<NosCore.GameObject.Services.GroupService.Group>(1);
            for (byte i = 0; i < (byte)(GroupType.Group + 1); i++)
            {
                var session = await World.CreateSessionAsync();
                var mockChannel = new Mock<IChannel>();
                mockChannel.Setup(s => s.Id).Returns(Guid.NewGuid().ToString());
                session.RegisterChannel(mockChannel.Object);
                Sessions.Add(i, session);
                var sessionGroupFactoryMock = new Mock<ISessionGroupFactory>();
                sessionGroupFactoryMock.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
                session.Character.Group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, sessionGroupFactoryMock.Object);
                session.Character.Group.JoinGroup(session.Character);
            }

            BlacklistHub = new Mock<IBlacklistHub>();
            PJoinPacketHandler = new PjoinPacketHandler(Logger, BlacklistHub.Object, World.Clock, idServer, World.LogLanguageLocalizer, World.GameLanguageLocalizer, World.SessionRegistry, new Mock<GameObject.Services.GroupService.ICoupleBuffService>().Object, new NosCore.GameObject.Services.BattleService.RespawnService(new(), new(), new()));
        }

        [TestMethod]
        public async Task AcceptGroupJoinRequestShouldSucceed()
        {
            await new Spec("Accept group join request should succeed")
                .Given(Session1HasGroupRequestFromSession0)
                .WhenAsync(Session0AcceptsSession1RequestAsync)
                .Then(BothSessionsShouldBeInSameGroup)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheOneWhoInvitedLeadsTheNewGroup()
        {
            await new Spec("The one who invited is told they lead the new group, and does")
                .Given(Session1HasGroupRequestFromSession0)
                .WhenAsync(Session0AcceptsSession1RequestAsync)
                .Then(Session1ShouldLeadTheGroup)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AcceptingASharedPointOfReturnTakesTheSharersOne()
        {
            await new Spec("A party member who accepts the shared point of return gets the sharer's")
                .GivenAsync(Session0And1AreInAPartyAsync)
                .And(Session0SavedAPointOfReturnAt_, (short)1, (short)80, (short)116)
                .WhenAsync(Session0SharesItsPointOfReturnAsync)
                .AndAsync(Session1AcceptsTheSharedPointAsync)
                .Then(Session1PointOfReturnShouldBe_, (short)1, (short)80, (short)116)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SharingAPointOfReturnInvitesNobodyIntoTheParty()
        {
            await new Spec("Asking members to take a point of return is not a party invitation they could accept later")
                .GivenAsync(Session0And1AreInAPartyAsync)
                .And(Session0SavedAPointOfReturnAt_, (short)1, (short)80, (short)116)
                .WhenAsync(Session0SharesItsPointOfReturnAsync)
                .Then(Session0ShouldHaveNoPendingInvitation)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SharingWithoutAPointOfReturnAsksNobody()
        {
            await new Spec("A sharer with no point of return is told so and no member is asked")
                .GivenAsync(Session0And1AreInAPartyAsync)
                .WhenAsync(Session0SharesItsPointOfReturnAsync)
                .Then(Session0ShouldBeTold_, Game18NConstString.CantSharePoint)
                .And(Session1ShouldHaveNoQuestion)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinFullGroupShouldFail()
        {
            await new Spec("Join full group should fail")
                .GivenAsync(GroupIsFullAsync)
                .WhenAsync(Session_RequestsToJoinAsync, 3)
                .Then(Session_ShouldNotJoinGroup, 3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AcceptNotRequestedGroupShouldFail()
        {
            await new Spec("Accept not requested group should fail")
                .WhenAsync(Session0AcceptsSession1RequestAsync)
                .Then(BothSessionsShouldBeAlone)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeclineNotRequestedGroupShouldFail()
        {
            await new Spec("Decline not requested group should fail")
                .WhenAsync(Session0DeclinesSession1RequestAsync)
                .Then(BothSessionsShouldBeAlone)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LastRequestNotNullAfterOne()
        {
            await new Spec("Last request not null after one")
                .WhenAsync(Session_InvitesMultipleSessionsAsync, 0)
                .Then(LastGroupRequestShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TwoRequestsLess5SecDelayShouldOnlyAddOne()
        {
            await new Spec("Two requests less 5 sec delay should only add one")
                .WhenAsync(Session_InvitesWithShortDelayAsync, 0)
                .Then(OnlyOneRequestShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TwoRequestsMore5SecDelayShouldAddBoth()
        {
            await new Spec("Two requests more 5 sec delay should add both")
                .WhenAsync(Session_InvitesWithLongDelayAsync, 0)
                .Then(TwoRequestsShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InvitingPlayerWhoBlocksGroupRequestsShouldBeRefused()
        {
            await new Spec("Inviting a player who blocks group requests should be refused")
                .Given(Session1BlocksGroupRequests)
                .WhenAsync(Session0InvitesSession1Async)
                .Then(Session0ShouldBeToldTargetBlocksGroups)
                .And(Session1ShouldNotBeInvited)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InvitingPlayerWhoAllowsGroupRequestsShouldSendInvite()
        {
            await new Spec("Inviting a player who allows group requests should send the invite")
                .Given(Session1AllowsGroupRequests)
                .WhenAsync(Session0InvitesSession1Async)
                .Then(Session1ShouldBeInvited)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InvitingPlayerWhoBlacklistedTheInviterShouldBeRefused()
        {
            await new Spec("Inviting a player who blacklisted the inviter should be refused")
                .Given(Session1BlacklistedSession0)
                .WhenAsync(Session0InvitesSession1Async)
                .Then(Session0ShouldBeToldOfTheBlacklist)
                .And(Session1ShouldNotBeInvited)
                .ExecuteAsync();
        }

        private void Session1BlacklistedSession0()
        {
            var targetId = Sessions[1].Character.VisualId;
            BlacklistHub.Setup(s => s.GetBlacklistedAsync(targetId))
                .ReturnsAsync(new List<CharacterRelationStatus> { new() { CharacterId = Sessions[0].Character.CharacterId } });
        }

        private void Session0ShouldBeToldOfTheBlacklist() =>
            Assert.AreEqual(Game18NConstString.AlreadyBlacklisted, Sessions[0].LastPackets.OfType<InfoiPacket>().Last().Message);

        private void Session1BlocksGroupRequests() => Sessions[1].Character.GroupRequestBlocked = true;

        private void Session1AllowsGroupRequests() => Sessions[1].Character.GroupRequestBlocked = false;

        private async Task Session0InvitesSession1Async()
        {
            await PJoinPacketHandler.ExecuteAsync(new PjoinPacket
            {
                RequestType = GroupRequestType.Invited,
                CharacterId = Sessions[1].Character.CharacterId
            }, Sessions[0]);
        }

        private void Session0ShouldBeToldTargetBlocksGroups()
        {
            var packet = Sessions[0].LastPackets.OfType<MsgiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.TargetGroupBlocked, packet.Message);
        }

        private void Session1ShouldNotBeInvited()
        {
            Assert.IsFalse(Sessions[1].LastPackets.OfType<Dlgi2Packet>().Any());
            Assert.IsFalse(Sessions[0].Character.GroupRequestCharacterIds.ContainsKey(Sessions[1].Character.CharacterId));
        }

        private void Session1ShouldBeInvited()
        {
            var packet = Sessions[1].LastPackets.OfType<Dlgi2Packet>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.GroupInvite, packet.Question);
        }

        private async Task Session0And1AreInAPartyAsync()
        {
            Session1HasGroupRequestFromSession0();
            await Session0AcceptsSession1RequestAsync();
            Sessions[0].LastPackets.Clear();
            Sessions[1].LastPackets.Clear();
        }

        private void Session0SavedAPointOfReturnAt_(short mapId, short x, short y) =>
            new NosCore.GameObject.Services.BattleService.RespawnService(new(), new(), new())
                .SetReturnPoint(Sessions[0].Character, mapId, x, y);

        private Task Session0SharesItsPointOfReturnAsync() =>
            PJoinPacketHandler.ExecuteAsync(new PjoinPacket { RequestType = GroupRequestType.Sharing }, Sessions[0]);

        private Task Session1AcceptsTheSharedPointAsync() =>
            PJoinPacketHandler.ExecuteAsync(new PjoinPacket
            {
                RequestType = GroupRequestType.AcceptedShare,
                CharacterId = Sessions[0].Character.CharacterId
            }, Sessions[1]);

        private void Session1PointOfReturnShouldBe_(short mapId, short x, short y)
        {
            var point = new NosCore.GameObject.Services.BattleService.RespawnService(new(), new(), new())
                .GetReturnPoint(Sessions[1].Character, mapId);
            Assert.AreEqual(((short)mapId, x, y), point);
        }

        private void Session0ShouldBeTold_(Game18NConstString message) =>
            Assert.AreEqual(message, Sessions[0].LastPackets.OfType<InfoiPacket>().Last().Message);

        private void Session0ShouldHaveNoPendingInvitation() =>
            Assert.IsTrue(Sessions[0].Character.GroupRequestCharacterIds.IsEmpty);

        private void Session1ShouldHaveNoQuestion() =>
            Assert.IsFalse(Sessions[1].LastPackets.OfType<Dlgi2Packet>().Any());

        private void Session1HasGroupRequestFromSession0()
        {
            Sessions[1].Character.GroupRequestCharacterIds
                .TryAdd(Sessions[0].Character.CharacterId, Sessions[0].Character.CharacterId);
        }

        private async Task GroupIsFullAsync()
        {
            for (var i = 1; i < 3; i++)
            {
                Sessions[i].Character.GroupRequestCharacterIds
                    .TryAdd(Sessions[0].Character.CharacterId, Sessions[0].Character.CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = Sessions[i].Character.CharacterId
                };

                await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
            }
        }

        private async Task Session0AcceptsSession1RequestAsync()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = Sessions[1].Character.CharacterId
            };

            await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
        }

        private async Task Session0DeclinesSession1RequestAsync()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = Sessions[1].Character.CharacterId
            };

            await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
        }

        private async Task Session_RequestsToJoinAsync(int value)
        {
            Sessions[3].Character.GroupRequestCharacterIds
                .TryAdd(Sessions[0].Character.CharacterId, Sessions[0].Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = Sessions[3].Character.CharacterId
            };

            await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
        }

        private async Task Session_InvitesMultipleSessionsAsync(int value)
        {
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = Sessions[i].Character.CharacterId
                };

                await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
            }
        }

        private async Task Session_InvitesWithShortDelayAsync(int value)
        {
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = Sessions[i].Character.CharacterId
                };
                World.Clock.AdvanceSeconds(1);
                await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
            }
        }

        private async Task Session_InvitesWithLongDelayAsync(int value)
        {
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = Sessions[i].Character.CharacterId
                };

                if (i == 2)
                {
                    World.Clock.AdvanceMinutes(6);
                }

                await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
            }
        }

        private void Session1ShouldLeadTheGroup()
        {
            Assert.IsTrue(Sessions[1].LastPackets.OfType<InfoiPacket>().Any(p => p.Message == Game18NConstString.YouAreNowGroupLeader));
            Assert.IsTrue(Sessions[1].Character.Group.IsGroupLeader(Sessions[1].Character.CharacterId));
            Assert.IsFalse(Sessions[0].Character.Group.IsGroupLeader(Sessions[0].Character.CharacterId));
        }

        private void BothSessionsShouldBeInSameGroup()
        {
            Assert.IsTrue((Sessions[0].Character.Group.Count > 1)
                && (Sessions[1].Character.Group.Count > 1)
                && (Sessions[0].Character.Group.GroupId
                    == Sessions[1].Character.Group.GroupId));
        }

        private void Session_ShouldNotJoinGroup(int value)
        {
            Assert.IsTrue(Sessions[3].Character.Group.Count == 1);
        }

        private void BothSessionsShouldBeAlone()
        {
            Assert.IsTrue((Sessions[0].Character.Group.Count == 1)
                && (Sessions[1].Character.Group.Count == 1));
        }

        private void LastGroupRequestShouldNotBeNull()
        {
            Assert.IsNotNull(Sessions[0].Character.LastGroupRequest);
        }

        private void OnlyOneRequestShouldExist()
        {
            Assert.IsTrue(Sessions[0].Character.GroupRequestCharacterIds.Count == 1);
        }

        private void TwoRequestsShouldExist()
        {
            Assert.IsTrue(Sessions[0].Character.GroupRequestCharacterIds.Count == 2);
        }
    }
}

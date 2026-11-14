//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Group;
using NosCore.Packets.ClientPackets.Groups;
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
    public class PleavePacketHandlerTests
    {
        private TestWorld World;
        private static readonly ILogger<PjoinPacketHandler> Logger = new Mock<ILogger<PjoinPacketHandler>>().Object;
        private readonly Dictionary<int, ClientSession> Sessions = new();
        private PjoinPacketHandler PJoinPacketHandler;
        private PleavePacketHandler PLeavePacketHandler;
        private GroupDisconnectHandler DisconnectHandler;

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

            var sessionGroupFactoryForHandler = new Mock<ISessionGroupFactory>();
            sessionGroupFactoryForHandler.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
            World.Inject<IIdService<NosCore.GameObject.Services.GroupService.Group>>(idServer);
            World.Inject<ISessionRegistry>(World.SessionRegistry);
            World.Inject(sessionGroupFactoryForHandler.Object);
            World.Inject(new Mock<GameObject.Services.GroupService.ICoupleBuffService>().Object);
            var groupLeaveService = World.Create<GroupLeaveService>();
            PLeavePacketHandler = new PleavePacketHandler(groupLeaveService);
            DisconnectHandler = new GroupDisconnectHandler(groupLeaveService);

            var mock = new Mock<IBlacklistHub>();
            PJoinPacketHandler = new PjoinPacketHandler(Logger, mock.Object, World.Clock, idServer, World.LogLanguageLocalizer, World.GameLanguageLocalizer, World.SessionRegistry, new Mock<GameObject.Services.GroupService.ICoupleBuffService>().Object, new NosCore.GameObject.Services.BattleService.RespawnService(new(), new(), new()));
        }

        [TestMethod]
        public async Task PleaveDoesNotTakeAMemberOutOfARaidTeam()
        {
            await new Spec("A raid team is only left through the raid window")
                .GivenAsync(TwoSessionsAreGroupedAsync)
                .And(TheGroupIsARaidTeam)
                .WhenAsync(Session_LeavesGroupAsync, 1)
                .Then(Session_IsStillInTheRaidTeam, 1)
                .ExecuteAsync();
        }

        private void TheGroupIsARaidTeam() =>
            Sessions[0].Character.Group.Raid = new NosCore.GameObject.Services.ScriptedInstanceService.ScriptedInstance();

        private void Session_IsStillInTheRaidTeam(int value)
        {
            Assert.IsNotNull(Sessions[value].Character.Group.Raid);
            Assert.AreEqual(2, Sessions[value].Character.Group.Count);
        }

        [TestMethod]
        public async Task LeaveGroupWhenNotGroupedShouldKeepSolo()
        {
            await new Spec("Leave group when not grouped should keep solo")
                .WhenAsync(Session_LeavesGroupAsync, 0)
                .Then(Session_ShouldBeAlone, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeaveGroupWhenGroupedShouldLeave()
        {
            await new Spec("Leave group when grouped should leave")
                .GivenAsync(SessionsAreInFullGroupAsync)
                .WhenAsync(Session_LeavesGroupAsync, 1)
                .Then(Session_ShouldBeAlone, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeaderLeavingShouldTransferLeadership()
        {
            await new Spec("Leader leaving should transfer leadership")
                .GivenAsync(SessionsAreInFullGroupAsync)
                .WhenAsync(Session_LeavesGroupAsync, 1)
                .Then(Session_ShouldBeLeader, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonLeaderLeavingThreePersonGroupShouldKeepLeader()
        {
            await new Spec("Non leader leaving three person group should keep leader")
                .GivenAsync(SessionsAreInFullGroupAsync)
                .WhenAsync(Session_LeavesGroupAsync, 0)
                .Then(Session_ShouldBeLeader, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheEndOfAGroupOfTwoIsAnnouncedOnceToEachMember()
        {
            await new Spec("Each member of a group of two hears once that it was disbanded")
                .GivenAsync(TwoSessionsAreGroupedAsync)
                .WhenAsync(Session_LeavesGroupAsync, 0)
                .Then(Session_ShouldHearOfTheDisbandingOnce, 0)
                .And(Session_ShouldHearOfTheDisbandingOnce, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeclineGroupJoinShouldKeepBothAlone()
        {
            await new Spec("Decline group join should keep both alone")
                .Given(Session1HasGroupRequestFromSession0)
                .WhenAsync(Session_DeclinesAndLeavesAsync, 0)
                .Then(BothSessionsShouldBeAlone)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingTwoPersonGroupShouldDissolve()
        {
            await new Spec("Leaving two person group should dissolve")
                .GivenAsync(TwoSessionsAreGroupedAsync)
                .WhenAsync(Session_LeavesGroupAsync, 0)
                .Then(BothSessionsShouldBeAlone)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DisconnectingFromATwoPersonGroupDissolvesIt()
        {
            await new Spec("Disconnecting from a two person group dissolves it")
                .GivenAsync(TwoSessionsAreGroupedAsync)
                .WhenAsync(Session_DisconnectsAsync, 1)
                .Then(Session_ShouldBeAlone, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLeaderDisconnectingHandsTheGroupOn()
        {
            await new Spec("The leader disconnecting hands the group on")
                .GivenAsync(SessionsAreInFullGroupAsync)
                .WhenAsync(Session_DisconnectsAsync, 1)
                .Then(Session_ShouldBeLeader, 0)
                .And(Session_ShouldBeInAGroupOf, 2)
                .ExecuteAsync();
        }

        private async Task Session_DisconnectsAsync(int value)
        {
            await DisconnectHandler.HandleDisconnectAsync(Sessions[value]);
        }

        private void Session_ShouldBeInAGroupOf(int count)
        {
            Assert.AreEqual(count, Sessions[0].Character.Group.Count);
        }

        private async Task SessionsAreInFullGroupAsync()
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

        private void Session1HasGroupRequestFromSession0()
        {
            Sessions[1].Character.GroupRequestCharacterIds
                .TryAdd(Sessions[0].Character.CharacterId, Sessions[0].Character.CharacterId);
        }

        private async Task TwoSessionsAreGroupedAsync()
        {
            Sessions[1].Character.GroupRequestCharacterIds
                .TryAdd(Sessions[0].Character.CharacterId, Sessions[0].Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = Sessions[1].Character.CharacterId
            };

            await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
        }

        private async Task Session_LeavesGroupAsync(int value)
        {
            await PLeavePacketHandler.ExecuteAsync(new PleavePacket(), Sessions[value]);
        }

        private async Task Session_DeclinesAndLeavesAsync(int value)
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = Sessions[1].Character.CharacterId
            };

            await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
            await PLeavePacketHandler.ExecuteAsync(new PleavePacket(), Sessions[0]);
        }

        private void Session_ShouldBeAlone(int value)
        {
            Assert.IsTrue(Sessions[value].Character.Group.Count == 1);
        }

        private void Session_ShouldBeLeader(int value)
        {
            Assert.IsTrue(Sessions[value].Character.Group
                .IsGroupLeader(Sessions[value].Character.CharacterId));
        }

        private void Session_ShouldHearOfTheDisbandingOnce(int value) =>
            Assert.AreEqual(1, Sessions[value].LastPackets.OfType<MsgiPacket>()
                .Count(p => p.Message == Game18NConstString.PartyDisbanded));

        private void BothSessionsShouldBeAlone()
        {
            Assert.IsTrue((Sessions[0].Character.Group.Count == 1)
                && (Sessions[1].Character.Group.Count == 1));
        }
    }
}

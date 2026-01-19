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
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Group;
using NosCore.Packets.ClientPackets.Groups;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Group
{
    [TestClass]
    public class PleavePacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Dictionary<int, ClientSession> Sessions = new();
        private PjoinPacketHandler PJoinPacketHandler = null!;
        private PleavePacketHandler PLeavePacketHandler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            var idServer = new IdService<NosCore.GameObject.Services.GroupService.Group>(1);
            for (byte i = 0; i < (byte)(GroupType.Group + 1); i++)
            {
                var session = await TestHelpers.Instance.GenerateSessionAsync();
                var mockChannel = new Mock<IChannel>();
                mockChannel.Setup(s => s.Id).Returns(Guid.NewGuid().ToString());
                session.RegisterChannel(mockChannel.Object);
                Sessions.Add(i, session);
                var sessionGroupFactoryMock = new Mock<ISessionGroupFactory>();
                sessionGroupFactoryMock.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
                session.Character.Group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, sessionGroupFactoryMock.Object);
                session.Character.Group.JoinGroup(session.Character);
            }

            PLeavePacketHandler = new PleavePacketHandler(idServer, TestHelpers.Instance.SessionRegistry);

            var mock = new Mock<IBlacklistHub>();
            PJoinPacketHandler = new PjoinPacketHandler(Logger, mock.Object, TestHelpers.Instance.Clock, idServer, TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.GameLanguageLocalizer, TestHelpers.Instance.SessionRegistry);
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
                .WhenAsync(Session_LeavesGroupAsync, 0)
                .Then(Session_ShouldBeLeader, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonLeaderLeavingThreePersonGroupShouldKeepLeader()
        {
            await new Spec("Non leader leaving three person group should keep leader")
                .GivenAsync(SessionsAreInFullGroupAsync)
                .WhenAsync(Session_LeavesGroupAsync, 1)
                .Then(Session_ShouldBeLeader, 0)
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
            Assert.IsTrue((Sessions[value].Character.Group != null) && (Sessions[value].Character.Group!.Count == 1));
        }

        private void Session_ShouldBeLeader(int value)
        {
            Assert.IsTrue(Sessions[value].Character.Group!
                .IsGroupLeader(Sessions[value].Character.CharacterId));
        }

        private void BothSessionsShouldBeAlone()
        {
            Assert.IsTrue((Sessions[0].Character.Group!.Count == 1)
                && (Sessions[1].Character.Group!.Count == 1));
        }
    }
}

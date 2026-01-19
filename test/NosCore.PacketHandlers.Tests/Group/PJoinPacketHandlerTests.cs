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
    public class PJoinPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Dictionary<int, ClientSession> Sessions = new();
        private PjoinPacketHandler PJoinPacketHandler = null!;

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

            var mock = new Mock<IBlacklistHub>();
            PJoinPacketHandler = new PjoinPacketHandler(Logger, mock.Object, TestHelpers.Instance.Clock, idServer, TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.GameLanguageLocalizer, TestHelpers.Instance.SessionRegistry);
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
                TestHelpers.Instance.Clock.AdvanceSeconds(1);
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
                    TestHelpers.Instance.Clock.AdvanceMinutes(6);
                }

                await PJoinPacketHandler.ExecuteAsync(pjoinPacket, Sessions[0]);
            }
        }

        private void BothSessionsShouldBeInSameGroup()
        {
            Assert.IsTrue((Sessions[0].Character.Group!.Count > 1)
                && (Sessions[1].Character.Group!.Count > 1)
                && (Sessions[0].Character.Group!.GroupId
                    == Sessions[1].Character.Group!.GroupId));
        }

        private void Session_ShouldNotJoinGroup(int value)
        {
            Assert.IsTrue(Sessions[3].Character.Group!.Count == 1);
        }

        private void BothSessionsShouldBeAlone()
        {
            Assert.IsTrue((Sessions[0].Character.Group!.Count == 1)
                && (Sessions[1].Character.Group!.Count == 1));
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

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
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Group
{
    [TestClass]
    public class GroupTalkPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Dictionary<int, ClientSession> _sessions = new();
        private GroupTalkPacketHandler _groupTalkPacketHandler = null!;
        private PjoinPacketHandler _pJoinPacketHandler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            var idServer = new IdService<NosCore.GameObject.Services.GroupService.Group>(1);

            for (var i = 0; i < 3; i++)
            {
                var session = await TestHelpers.Instance.GenerateSessionAsync();
                var mockChannel = new Mock<IChannel>();
                mockChannel.Setup(s => s.Id).Returns(Guid.NewGuid().ToString());
                session.RegisterChannel(mockChannel.Object);
                _sessions.Add(i, session);
                var sessionGroupFactoryMock = new Mock<ISessionGroupFactory>();
                sessionGroupFactoryMock.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
                session.Character.Group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, sessionGroupFactoryMock.Object);
                session.Character.Group.JoinGroup(session.Character);
            }

            _groupTalkPacketHandler = new GroupTalkPacketHandler();

            var mock = new Mock<IBlacklistHub>();
            _pJoinPacketHandler = new PjoinPacketHandler(
                Logger,
                mock.Object,
                TestHelpers.Instance.Clock,
                idServer,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.GameLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task GroupTalkWhenAloneShouldNotSendMessage()
        {
            await new Spec("Group talk when alone should not send message")
                .WhenAsync(Session0SendsGroupMessage)
                .Then(NoSpkPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GroupTalkWhenInGroupShouldSendMessage()
        {
            await new Spec("Group talk when in group should send message")
                .GivenAsync(TwoSessionsAreGroupedAsync)
                .WhenAsync(Session0SendsGroupMessage)
                .Then(SpkPacketShouldBeSentToGroup)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GroupTalkShouldHaveCorrectSpeakType()
        {
            await new Spec("Group talk should have correct speak type")
                .GivenAsync(TwoSessionsAreGroupedAsync)
                .WhenAsync(Session0SendsGroupMessage)
                .Then(SpkPacketShouldHaveGroupSpeakType)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GroupTalkShouldContainCorrectMessage()
        {
            await new Spec("Group talk should contain correct message")
                .GivenAsync(TwoSessionsAreGroupedAsync)
                .WhenAsync(Session0SendsSpecificMessage)
                .Then(SpkPacketShouldContainCorrectMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GroupTalkInFullGroupShouldWork()
        {
            await new Spec("Group talk in full group should work")
                .GivenAsync(ThreeSessionsAreGroupedAsync)
                .WhenAsync(Session0SendsGroupMessage)
                .Then(SpkPacketShouldBeSentToGroup)
                .ExecuteAsync();
        }

        private async Task TwoSessionsAreGroupedAsync()
        {
            _sessions[1].Character.GroupRequestCharacterIds
                .TryAdd(_sessions[0].Character.CharacterId, _sessions[0].Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _sessions[1].Character.CharacterId
            };

            await _pJoinPacketHandler.ExecuteAsync(pjoinPacket, _sessions[0]);
        }

        private async Task ThreeSessionsAreGroupedAsync()
        {
            for (var i = 1; i < 3; i++)
            {
                _sessions[i].Character.GroupRequestCharacterIds
                    .TryAdd(_sessions[0].Character.CharacterId, _sessions[0].Character.CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _sessions[i].Character.CharacterId
                };

                await _pJoinPacketHandler.ExecuteAsync(pjoinPacket, _sessions[0]);
            }
        }

        private async Task Session0SendsGroupMessage()
        {
            var packet = new GroupTalkPacket
            {
                Message = "Hello Group!"
            };
            await _groupTalkPacketHandler.ExecuteAsync(packet, _sessions[0]);
        }

        private async Task Session0SendsSpecificMessage()
        {
            var packet = new GroupTalkPacket
            {
                Message = "Test Message 123"
            };
            await _groupTalkPacketHandler.ExecuteAsync(packet, _sessions[0]);
        }

        private void NoSpkPacketShouldBeSent()
        {
            var packet = _sessions[0].Character.Group!.LastPackets.FirstOrDefault(s => s is SpeakPacket);
            Assert.IsNull(packet);
        }

        private void SpkPacketShouldBeSentToGroup()
        {
            var packet = _sessions[0].Character.Group!.LastPackets.FirstOrDefault(s => s is SpeakPacket);
            Assert.IsNotNull(packet);
        }

        private void SpkPacketShouldHaveGroupSpeakType()
        {
            var packet = (SpeakPacket?)_sessions[0].Character.Group!.LastPackets.FirstOrDefault(s => s is SpeakPacket);
            Assert.IsNotNull(packet);
            Assert.AreEqual(SpeakType.Group, packet.SpeakType);
        }

        private void SpkPacketShouldContainCorrectMessage()
        {
            var packet = (SpeakPacket?)_sessions[0].Character.Group!.LastPackets.FirstOrDefault(s => s is SpeakPacket);
            Assert.IsNotNull(packet);
            Assert.AreEqual("Test Message 123", packet.Message);
        }
    }
}

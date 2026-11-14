//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RaidService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.PacketHandlers.Raid;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Raid
{
    [TestClass]
    public class RdPacketHandlerTests
    {
        private const long Other = 99;

        private TestWorld World;
        private Mock<IRaidService> _raids;
        private ClientSession _session;
        private RdPacketHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _raids = World.Freeze<Mock<IRaidService>>();
            _session = await World.CreateSessionAsync();
            _handler = World.Create<RdPacketHandler>();
        }

        [TestMethod]
        public async Task ALeaderNamingAPlayerInvitesThem()
        {
            await new Spec("A team leader's rd 1 with no answer invites the named player")
                .Given(TheSessionLeadsARaidTeam)
                .WhenAsync(TheSessionSends_, new RdPacket { Type = 1, CharacterId = Other })
                .Then(TheRaidServiceWasAskedTo_, "invite")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnsweringAnInvitationJoins()
        {
            await new Spec("A player outside any team answering rd 1 joins the leader")
                .WhenAsync(TheSessionSends_, new RdPacket { Type = 1, CharacterId = Other, Parameter = 1 })
                .Then(TheRaidServiceWasAskedTo_, "join")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeaveKickAndDisbandReachTheirRules()
        {
            await new Spec("rd 2, 3 and 4 leave, kick and disband")
                .WhenAsync(TheSessionSends_, new RdPacket { Type = 2, CharacterId = Other })
                .AndAsync(TheSessionSends_, new RdPacket { Type = 3, CharacterId = Other })
                .AndAsync(TheSessionSends_, new RdPacket { Type = 4, CharacterId = Other })
                .Then(TheRaidServiceWasAskedTo_, "leave")
                .And(TheRaidServiceWasAskedTo_, "kick")
                .And(TheRaidServiceWasAskedTo_, "disband")
                .ExecuteAsync();
        }

        private void TheSessionLeadsARaidTeam() => _session.Character.Group.Raid = new ScriptedInstance();

        private void TheRaidServiceWasAskedTo_(string request)
        {
            switch (request)
            {
                case "invite":
                    _raids.Verify(s => s.InviteAsync(_session, Other), Times.Once);
                    _raids.Verify(s => s.JoinAsync(It.IsAny<ClientSession>(), It.IsAny<long>()), Times.Never);
                    break;
                case "join":
                    _raids.Verify(s => s.JoinAsync(_session, Other), Times.Once);
                    _raids.Verify(s => s.InviteAsync(It.IsAny<ClientSession>(), It.IsAny<long>()), Times.Never);
                    break;
                case "leave":
                    _raids.Verify(s => s.LeaveAsync(_session), Times.Once);
                    break;
                case "kick":
                    _raids.Verify(s => s.KickAsync(_session, Other), Times.Once);
                    break;
                default:
                    _raids.Verify(s => s.DisbandAsync(_session), Times.Once);
                    break;
            }
        }

        private Task TheSessionSends_(RdPacket packet) => _handler.ExecuteAsync(packet, _session);
    }
}

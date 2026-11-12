//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.PacketHandlers.Family;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class GListPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IFamilyManagementService> _familyManagementService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _familyManagementService = new Mock<IFamilyManagementService>();
        }

        [TestMethod]
        public async Task OpeningTheMemberListSendsIt()
        {
            await new Spec("Opening the member list sends it")
                .WhenAsync(TheClientSends, GListPacketType.MemberList)
                .Then(TheMemberListShouldBeSent_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OpeningFamilyManagementSendsNothing()
        {
            await new Spec("Opening family management sends nothing")
                .WhenAsync(TheClientSends, GListPacketType.FamilyManagement)
                .Then(TheMemberListShouldBeSent_Times, 0)
                .ExecuteAsync();
        }

        private Task TheClientSends(GListPacketType type) =>
            new GListPacketHandler(_familyManagementService.Object).ExecuteAsync(new GListPacket { Type = type }, _session);

        private void TheMemberListShouldBeSent_Times(int times) =>
            _familyManagementService.Verify(s => s.SendMemberListAsync(_session), Times.Exactly(times));
    }
}

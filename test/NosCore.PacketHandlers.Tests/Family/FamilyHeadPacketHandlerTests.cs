//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.PacketHandlers.Family;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyHeadPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IFamilyManagementService> Management;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Management = World.Freeze<Mock<IFamilyManagementService>>();
        }

        [TestMethod]
        public async Task HandingOverHeadshipAsksForTheHeadAuthority()
        {
            await new Spec("Handing over headship asks for the head authority")
                .WhenAsync(HandingTheFamilyTo_, "heir")
                .Then(_ShouldBeMadeHead, "heir")
                .ExecuteAsync();
        }

        private Task HandingTheFamilyTo_(string name) =>
            World.Create<FamilyHeadPacketHandler>().ExecuteAsync(new FamilyHeadPacket { CharacterName = name }, Session);

        private void _ShouldBeMadeHead(string name) =>
            Management.Verify(s => s.ChangeAuthorityAsync(Session, name, FamilyAuthority.Head), Times.Once);
    }
}

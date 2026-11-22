//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RaidService;
using NosCore.PacketHandlers.Raid;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Raid
{
    [TestClass]
    public class RlPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IRaidService> Raids;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Raids = World.Freeze<Mock<IRaidService>>();
        }

        [TestMethod]
        public async Task OpeningTheBoardShowsIt()
        {
            await new Spec("Opening the board shows it")
                .WhenAsync(Sending_, RlPacketType.Normal, (string?)null)
                .Then(TheBoardIsShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringListsTheRaid()
        {
            await new Spec("Registering lists the raid")
                .WhenAsync(Sending_, RlPacketType.WantToRegister, (string?)null)
                .Then(TheRaidIsListed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisteringUnlistsTheRaid()
        {
            await new Spec("Unregistering unlists the raid")
                .WhenAsync(Sending_, RlPacketType.WantToUnregister, (string?)null)
                .Then(TheRaidIsUnlisted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingALeaderJoinsTheirRaid()
        {
            await new Spec("Picking a leader joins their raid")
                .WhenAsync(Sending_, RlPacketType.MemberAndAlreadyInList, (string?)"leader")
                .Then(TheLeadersRaidIsJoined)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingNoLeaderJoinsNothing()
        {
            await new Spec("Picking no leader joins nothing")
                .WhenAsync(Sending_, RlPacketType.MemberAndAlreadyInList, (string?)null)
                .Then(NoRaidIsJoined)
                .ExecuteAsync();
        }

        private Task Sending_(RlPacketType type, string? characterName) =>
            World.Create<RlPacketHandler>().ExecuteAsync(new RlPacket { Type = (short)type, CharacterName = characterName }, Session);

        private void TheBoardIsShown() => Raids.Verify(s => s.ShowBoardAsync(Session), Times.Once);

        private void TheRaidIsListed() => Raids.Verify(s => s.ListAsync(Session), Times.Once);

        private void TheRaidIsUnlisted() => Raids.Verify(s => s.UnlistAsync(Session), Times.Once);

        private void TheLeadersRaidIsJoined() => Raids.Verify(s => s.JoinListedAsync(Session, "leader"), Times.Once);

        private void NoRaidIsJoined() =>
            Raids.Verify(s => s.JoinListedAsync(It.IsAny<ClientSession>(), It.IsAny<string>()), Times.Never);
    }
}

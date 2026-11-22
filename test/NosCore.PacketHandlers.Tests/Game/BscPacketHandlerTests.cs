//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RainbowBattleService;
using NosCore.GameObject.Services.TalentArenaService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Arena;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class BscPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<ITalentArenaService> TalentArena;
        private Mock<IRainbowBattleService> RainbowBattle;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            TalentArena = World.Freeze<Mock<ITalentArenaService>>();
            RainbowBattle = World.Freeze<Mock<IRainbowBattleService>>();
        }

        [TestMethod]
        public async Task CancellingTheTalentArenaAsksForConfirmationFirst()
        {
            await new Spec("Cancelling the talent arena asks for confirmation first")
                .WhenAsync(Cancelling_WithOption_, BscPacketType.TalentArena, (byte?)null)
                .Then(TheTalentArenaIsCancelledWithConfirmation_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingTheTalentArenaCancelIsPassedOn()
        {
            await new Spec("Confirming the talent arena cancel is passed on")
                .WhenAsync(Cancelling_WithOption_, BscPacketType.TalentArena, (byte?)1)
                .Then(TheTalentArenaIsCancelledWithConfirmation_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CancellingTheArenaOfMastersNeedsNoConfirmation()
        {
            await new Spec("Cancelling the arena of masters needs no confirmation")
                .WhenAsync(Cancelling_WithOption_, BscPacketType.ArenaOfMasters, (byte?)null)
                .Then(TheTalentArenaIsCancelledWithConfirmation_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CancellingRainbowBattleLeavesItsQueue()
        {
            await new Spec("Cancelling rainbow battle leaves its queue")
                .WhenAsync(Cancelling_WithOption_, BscPacketType.RainbowBattle, (byte?)null)
                .Then(RainbowBattleIsCancelled)
                .And(TheTalentArenaIsNotCancelled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CancellingAMinigameWithNoQueueDoesNothing()
        {
            await new Spec("Cancelling a minigame with no queue does nothing")
                .WhenAsync(Cancelling_WithOption_, BscPacketType.SheepFarm, (byte?)null)
                .Then(TheTalentArenaIsNotCancelled)
                .And(RainbowBattleIsNotCancelled)
                .ExecuteAsync();
        }

        private Task Cancelling_WithOption_(BscPacketType type, byte? option) =>
            World.Create<BscPacketHandler>().ExecuteAsync(new BscPacket { Type = type, Option = option }, Session);

        private void TheTalentArenaIsCancelledWithConfirmation_(bool confirmed) =>
            TalentArena.Verify(s => s.CancelAsync(Session, confirmed), Times.Once);

        private void TheTalentArenaIsNotCancelled() =>
            TalentArena.Verify(s => s.CancelAsync(It.IsAny<ClientSession>(), It.IsAny<bool>()), Times.Never);

        private void RainbowBattleIsCancelled() => RainbowBattle.Verify(s => s.CancelAsync(Session), Times.Once);

        private void RainbowBattleIsNotCancelled() =>
            RainbowBattle.Verify(s => s.CancelAsync(It.IsAny<ClientSession>()), Times.Never);
    }
}

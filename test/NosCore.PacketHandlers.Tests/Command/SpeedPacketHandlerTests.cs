//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SpeedPacketHandlerTests
    {
        private SpeedPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new SpeedPacketHandler();
        }

        [TestMethod]
        public async Task SpeedWithValidValueShouldSetVehicleSpeed()
        {
            await new Spec("Speed with valid value should set vehicle speed")
                .WhenAsync(SettingSpeed_, 30)
                .Then(VehicleSpeedShouldBe_, 30)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpeedWithZeroShouldShowHelp()
        {
            await new Spec("Speed with zero should show help")
                .WhenAsync(SettingSpeed_, 0)
                .Then(ShouldShowHelpMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpeedWithNegativeShouldShowHelp()
        {
            await new Spec("Speed with value 60 or more should show help")
                .WhenAsync(SettingSpeed_, 60)
                .Then(ShouldShowHelpMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpeedWith59ShouldSetSpeed()
        {
            await new Spec("Speed with 59 should set speed")
                .WhenAsync(SettingSpeed_, 59)
                .Then(VehicleSpeedShouldBe_, 59)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpeedShouldSendPacketResponse()
        {
            await new Spec("Speed should send packet response")
                .WhenAsync(SettingSpeed_, 30)
                .Then(ShouldSendPacketResponse)
                .ExecuteAsync();
        }

        private async Task SettingSpeed_(int speed)
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new SpeedPacket { Speed = (byte)speed }, Session);
        }

        private void VehicleSpeedShouldBe_(int expectedSpeed)
        {
            Assert.AreEqual((byte)expectedSpeed, Session.Character.VehicleSpeed);
        }

        private void ShouldShowHelpMessage()
        {
            Assert.IsTrue(Session.LastPackets.Any(p => p is SayPacket));
        }

        private void ShouldSendPacketResponse()
        {
            Assert.IsTrue(Session.LastPackets.Count > 0);
        }
    }
}

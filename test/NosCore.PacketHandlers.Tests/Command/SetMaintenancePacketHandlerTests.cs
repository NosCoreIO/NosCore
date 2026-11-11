//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SetMaintenancePacketHandlerTests
    {
        private TestWorld World;
        private SetMaintenancePacketHandler Handler;
        private ClientSession Session;
        private Mock<IChannelHub> ChannelHub;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            ChannelHub = World.Freeze<Mock<IChannelHub>>();
            Handler = World.Create<SetMaintenancePacketHandler>();
        }

        [TestMethod]
        public async Task SetMaintenanceForwardsGlobalAndModeFlagsToChannelHub()
        {
            await new Spec("Set-maintenance forwards IsGlobal + MaintenanceMode flags through to IChannelHub.SetMaintenance")
                .WhenAsync(SettingMaintenance_, true, true)
                .Then(ChannelHubSetMaintenanceShouldBeCalledWith_, true, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SetMaintenanceCanDisableAndScopeLocally()
        {
            await new Spec("Disabling maintenance locally forwards (false, false) to the hub")
                .WhenAsync(SettingMaintenance_, false, false)
                .Then(ChannelHubSetMaintenanceShouldBeCalledWith_, false, false)
                .ExecuteAsync();
        }

        private async Task SettingMaintenance_(bool isGlobal, bool mode)
        {
            await Handler.ExecuteAsync(new SetMaintenancePacket
            {
                IsGlobal = isGlobal,
                MaintenanceMode = mode,
            }, Session);
        }

        private void ChannelHubSetMaintenanceShouldBeCalledWith_(bool expectedGlobal, bool expectedMode)
        {
            ChannelHub.Verify(x => x.SetMaintenance(expectedGlobal, expectedMode), Times.Once);
        }
    }
}

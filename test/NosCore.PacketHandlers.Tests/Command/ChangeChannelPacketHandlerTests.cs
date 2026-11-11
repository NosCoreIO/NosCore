//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ChannelService;
using NosCore.PacketHandlers.Command;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class ChangeChannelPacketHandlerTests
    {
        private TestWorld World;
        private ChangeChannelPacketHandler Handler;
        private ClientSession Session;
        private Mock<IChannelService> ChannelService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            ChannelService = new Mock<IChannelService>();
            Handler = new ChangeChannelPacketHandler(ChannelService.Object);
        }

        [TestMethod]
        public async Task ChangeChannelForwardsTheRequestedChannelIdToChannelService()
        {
            await new Spec("Change-channel forwards the packet's ChannelId through to IChannelService.MoveChannelAsync")
                .WhenAsync(ChangingChannelTo_, 3)
                .Then(MoveChannelAsyncShouldBeCalledWith_, 3)
                .ExecuteAsync();
        }

        private async Task ChangingChannelTo_(int channelId)
        {
            await Handler.ExecuteAsync(new ChangeChannelPacket { ChannelId = (byte)channelId }, Session);
        }

        private void MoveChannelAsyncShouldBeCalledWith_(int expectedChannelId)
        {
            ChannelService.Verify(x => x.MoveChannelAsync(Session, expectedChannelId), Times.Once);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class ChannelHubTests
    {
        private ChannelHub Hub;
        private List<ChannelInfo> Registered = [];

        [TestInitialize]
        public async Task SetupAsync()
        {
            var world = await TestWorld.CreateAsync();
            world.Inject(new MasterClientList());
            Hub = world.Create<ChannelHub>();
        }

        [TestMethod]
        public async Task ARegisteredChannelKeepsItsServer()
        {
            await new Spec("A registered channel keeps its server")
                .Given(TwoServersRegisterAnAct4Channel)
                .WhenAsync(ListingTheChannels)
                .Then(TheAct4ChannelOfServer_ShouldBeOnPort_, (byte)1, (ushort)1338)
                .And(TheAct4ChannelOfServer_ShouldBeOnPort_, (byte)2, (ushort)2338)
                .ExecuteAsync();
        }

        private void TwoServersRegisterAnAct4Channel()
        {
            Hub.BindChannel("act4-1", new Channel
            {
                ClientName = "act4-1", Host = "127.0.0.1", Port = 1338, ServerId = 1, IsAct4 = true,
                ClientType = ServerType.WorldServer,
            });
            Hub.BindChannel("act4-2", new Channel
            {
                ClientName = "act4-2", Host = "127.0.0.1", Port = 2338, ServerId = 2, IsAct4 = true,
                ClientType = ServerType.WorldServer,
            });
        }

        private async Task ListingTheChannels()
        {
            Registered = await Hub.GetCommunicationChannels();
        }

        private void TheAct4ChannelOfServer_ShouldBeOnPort_(byte serverId, ushort port)
        {
            Assert.AreEqual(port, Registered.Single(c => c.IsAct4 && c.ServerId == serverId).Port);
        }
    }
}

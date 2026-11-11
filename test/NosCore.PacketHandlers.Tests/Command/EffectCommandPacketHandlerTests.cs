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
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class EffectCommandPacketHandlerTests
    {
        private TestWorld World;
        private EffectCommandPackettHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            Handler = new EffectCommandPackettHandler();
        }

        [TestMethod]
        public async Task EffectShouldSendPacket()
        {
            await new Spec("Effect should send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingEffectCommand_, 1)
                .Then(ShouldSendPacketToMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EffectWithDifferentIdShouldSendPacket()
        {
            await new Spec("Effect with different id should send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingEffectCommand_, 100)
                .Then(ShouldSendPacketToMap)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
            Session.Character.MapInstance.LastPackets.Clear();
        }

        private async Task ExecutingEffectCommand_(int effectId)
        {
            await Handler.ExecuteAsync(new EffectCommandPacket { EffectId = effectId }, Session);
        }

        private void ShouldSendPacketToMap()
        {
            Assert.IsTrue(Session.Character.MapInstance.LastPackets.Any());
        }
    }
}

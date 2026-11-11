//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SetHairStylePacketHandlerTests
    {
        private TestWorld World;
        private SetHairStylePacketHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(1);
            Handler = new SetHairStylePacketHandler();
        }

        [TestMethod]
        public async Task ValidHairStyleShouldBeApplied()
        {
            await new Spec("Valid hair style should be applied")
                .WhenAsync(SettingHairStyle_, (int)(byte)HairStyleType.HairStyleB)
                .Then(HairStyleShouldBe_, (int)(byte)HairStyleType.HairStyleB)
                .And(EqShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnknownHairStyleShouldBeIgnored()
        {
            await new Spec("Unknown hair style should be ignored")
                .Given(PacketsAreCleared)
                .WhenAsync(SettingHairStyle_, 200)
                .Then(NothingShouldBeSent)
                .ExecuteAsync();
        }

        private void PacketsAreCleared() => Session.LastPackets.Clear();

        private Task SettingHairStyle_(int value) =>
            Handler.ExecuteAsync(new SetHairStylePacket { Style = (byte)value }, Session);

        private void HairStyleShouldBe_(int value) =>
            Assert.AreEqual((HairStyleType)value, Session.Character.HairStyle);

        private void EqShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.OfType<EqPacket>().Any());

        private void NothingShouldBeSent() =>
            Assert.IsFalse(Session.LastPackets.OfType<EqPacket>().Any());
    }
}

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
    public class SetHairColorPacketHandlerTests
    {
        private SetHairColorPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Handler = new SetHairColorPacketHandler();
        }

        [TestMethod]
        public async Task ValidHairColorShouldBeApplied()
        {
            await new Spec("Valid hair color should be applied")
                .WhenAsync(SettingHairColor_, (int)(byte)HairColorType.Black)
                .Then(HairColorShouldBe_, (int)(byte)HairColorType.Black)
                .And(EqShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnknownHairColorShouldBeIgnored()
        {
            await new Spec("Unknown hair color should be ignored")
                .Given(PacketsAreCleared)
                .WhenAsync(SettingHairColor_, 200)
                .Then(NothingShouldBeSent)
                .ExecuteAsync();
        }

        private void PacketsAreCleared() => Session.LastPackets.Clear();

        private Task SettingHairColor_(int value) =>
            Handler.ExecuteAsync(new SetHairColorPacket { Color = (byte)value }, Session);

        private void HairColorShouldBe_(int value) =>
            Assert.AreEqual((HairColorType)value, Session.Character.HairColor);

        private void EqShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.OfType<EqPacket>().Any());

        private void NothingShouldBeSent() =>
            Assert.IsFalse(Session.LastPackets.OfType<EqPacket>().Any());
    }
}

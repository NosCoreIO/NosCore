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
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SetSpPointPacketHandlerTests
    {
        private TestWorld World;
        private SetSpPointPacketHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.SpAdditionPoint = 50;
            Handler = World.Create<SetSpPointPacketHandler>();
        }

        [TestMethod]
        public async Task TheGivenPointsAreSetAndShown()
        {
            await new Spec("The given specialist points are set and shown")
                .WhenAsync(SettingSpPoints_, 1234)
                .Then(TheSpBarShouldShow_, 1234)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NegativePointsAreSetToZero()
        {
            await new Spec("Negative specialist points are set to zero")
                .WhenAsync(SettingSpPoints_, -5)
                .Then(TheSpBarShouldShow_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PointsAboveTheCapStopAtTheCap()
        {
            await new Spec("Specialist points above the cap stop at the cap")
                .WhenAsync(SettingSpPoints_, 50_000)
                .Then(TheSpBarShouldShow_, 10_000)
                .ExecuteAsync();
        }

        private Task SettingSpPoints_(int value) =>
            Handler.ExecuteAsync(new SetSpPointPacket { SpPoint = value }, Session);

        private void TheSpBarShouldShow_(int value)
        {
            Assert.AreEqual(value, Session.Character.SpPoint);
            PacketAssert.AreEqual(new SpPacket
            {
                AdditionalPoint = 50,
                MaxAdditionalPoint = 1_000_000,
                SpPoint = value,
                MaxSpPoint = 10_000,
            }, Session.LastPackets.OfType<SpPacket>().Single());
        }
    }
}

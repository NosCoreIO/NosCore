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
    public class SetSpAdditionPointPacketHandlerTests
    {
        private TestWorld World;
        private SetSpAdditionPointPacketHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.SpPoint = 70;
            Handler = World.Create<SetSpAdditionPointPacketHandler>();
        }

        [TestMethod]
        public async Task TheGivenPointsAreSetAndShown()
        {
            await new Spec("The given additional specialist points are set and shown")
                .WhenAsync(SettingAdditionalPoints_, 4321)
                .Then(TheSpBarShouldShow_, 4321)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NegativePointsAreSetToZero()
        {
            await new Spec("Negative additional specialist points are set to zero")
                .WhenAsync(SettingAdditionalPoints_, -1)
                .Then(TheSpBarShouldShow_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PointsAboveTheCapStopAtTheCap()
        {
            await new Spec("Additional specialist points above the cap stop at the cap")
                .WhenAsync(SettingAdditionalPoints_, 2_000_000)
                .Then(TheSpBarShouldShow_, 1_000_000)
                .ExecuteAsync();
        }

        private Task SettingAdditionalPoints_(int value) =>
            Handler.ExecuteAsync(new SetSpAdditionPointPacket { SpAdditionPoint = value }, Session);

        private void TheSpBarShouldShow_(int value)
        {
            Assert.AreEqual(value, Session.Character.SpAdditionPoint);
            PacketAssert.AreEqual(new SpPacket
            {
                AdditionalPoint = value,
                MaxAdditionalPoint = 1_000_000,
                SpPoint = 70,
                MaxSpPoint = 10_000,
            }, Session.LastPackets.OfType<SpPacket>().Single());
        }
    }
}

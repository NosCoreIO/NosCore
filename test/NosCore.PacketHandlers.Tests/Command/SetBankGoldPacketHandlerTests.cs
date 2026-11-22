//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SetBankGoldPacketHandlerTests
    {
        private TestWorld World;
        private SetBankGoldPacketHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Handler = World.Create<SetBankGoldPacketHandler>();
        }

        [TestMethod]
        public async Task TheGivenAmountIsPutInTheBank()
        {
            await new Spec("The given amount is put in the bank")
                .WhenAsync(SettingBankGold_, 5_000_000)
                .Then(BankGoldShouldBe_, 5_000_000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANegativeAmountEmptiesTheBank()
        {
            await new Spec("A negative amount empties the bank")
                .Given(TheBankHolds_, 300)
                .WhenAsync(SettingBankGold_, -10)
                .Then(BankGoldShouldBe_, 0)
                .ExecuteAsync();
        }

        private void TheBankHolds_(int value) => Session.Character.BankGold = value;

        private Task SettingBankGold_(int value) =>
            Handler.ExecuteAsync(new SetBankGoldPacket { BankGold = value }, Session);

        private void BankGoldShouldBe_(int value) => Assert.AreEqual((long)value, Session.Character.BankGold);
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.BankService;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bank;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class OpenBankHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private OpenBankHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            _session.Character.Reputation = 0;
            _handler = new OpenBankHandler(new BankService(), World.NpcDialogService);
        }

        [TestMethod]
        public async Task RunnerPropertyMatchesOpenBankFacilities()
        {
            await new Spec("OpenBankHandler declares NrunRunnerType.OpenBankFacilities")
                .Then(RunnerShouldBe_, NrunRunnerType.OpenBankFacilities)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TalkingToTheBankerOpensTheBankWindow()
        {
            await new Spec("Use Bank Facilities opens the bank window with the balance, rank and fee")
                .Given(CharacterHasGold_AndBankGold_, 5_000_000L, 1_500_000L)
                .WhenAsync(NrunIsHandledWithAnNpc)
                .Then(BankWindowShouldOpenWith_Units_Rank_AndFee, 1_500L, BankRankType.Bronze3, 528_000)
                .And(TheBankWindowShouldBeRecorded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayerTakenForTheBankerOpensNothing()
        {
            await new Spec("A player given as the banker opens no bank window")
                .WhenAsync(NrunIsHandledWithAPlayerAsTheNpc)
                .Then(NoBankWindowShouldOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NullTargetIsIgnored()
        {
            await new Spec("A null target opens nothing")
                .WhenAsync(NrunIsHandledWithoutTarget)
                .Then(NoBankWindowShouldOpen)
                .ExecuteAsync();
        }

        private void RunnerShouldBe_(NrunRunnerType expected) => Assert.AreEqual(expected, _handler.Runner);

        private void CharacterHasGold_AndBankGold_(long gold, long bankGold)
        {
            _session.Character.Gold = gold;
            _session.Character.BankGold = bankGold;
        }

        private static NrunPacket Packet => new()
        {
            Runner = NrunRunnerType.OpenBankFacilities,
            VisualType = VisualType.Npc,
            VisualId = 1,
        };

        private Task NrunIsHandledWithAnNpc() =>
            _handler.HandleAsync(_session, _session.WithNpcAt(1, 1, 1), Packet);

        private Task NrunIsHandledWithAPlayerAsTheNpc() => _handler.HandleAsync(_session, _session.Character, Packet);

        private void TheBankWindowShouldBeRecorded() =>
            Assert.IsTrue(World.NpcDialogService.IsOpen(_session.Character, NpcDialogWindow.Bank));

        private Task NrunIsHandledWithoutTarget() => _handler.HandleAsync(_session, null, Packet);

        private void BankWindowShouldOpenWith_Units_Rank_AndFee(long units, BankRankType rank, int fee)
        {
            var gb = _session.LastPackets.OfType<GbPacket>().Single();
            Assert.AreEqual(BankActionType.OpenBank, gb.Type);
            Assert.AreEqual(units, gb.GoldBank);
            Assert.AreEqual(5_000_000, gb.Gold);
            Assert.AreEqual(rank, gb.BankRank);
            Assert.AreEqual(fee, gb.BankTax);
        }

        private void NoBankWindowShouldOpen() =>
            Assert.IsFalse(_session.LastPackets.OfType<GbPacket>().Any());
    }
}

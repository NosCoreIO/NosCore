//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class GetSavingsBookHandlerTests
    {
        private const short SavingsBookVNum = 5836;
        private TestWorld World;
        private ClientSession _session;
        private GetSavingsBookHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var items = new List<ItemDto>
            {
                new Item { VNum = SavingsBookVNum, Type = NoscorePocketType.Main, Effect = ItemEffectType.CuarryBankSavingsBook },
            };
            _handler = new GetSavingsBookHandler(new ItemGenerationService(items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService()),
                new ItemCatalog(items));
        }

        [TestMethod]
        public async Task RunnerPropertyMatchesGetSavingsBook()
        {
            await new Spec("GetSavingsBookHandler declares NrunRunnerType.GetSavingsBook")
                .Then(RunnerShouldBe_, NrunRunnerType.GetSavingsBook)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BankerHandsOutTheSavingsBook()
        {
            await new Spec("Asking the banker for a savings book puts one in the inventory")
                .WhenAsync(NrunIsHandledWithAnNpc)
                .Then(SavingsBookCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyOneSavingsBookIsGiven()
        {
            await new Spec("Asking again while holding a savings book gives no second one")
                .WhenAsync(NrunIsHandledWithAnNpc)
                .AndAsync(NrunIsHandledWithAnNpc)
                .Then(SavingsBookCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayerTakenForTheBankerGivesNothing()
        {
            await new Spec("A player given as the banker hands out no savings book")
                .WhenAsync(NrunIsHandledWithAPlayerAsTheNpc)
                .Then(SavingsBookCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NullTargetIsIgnored()
        {
            await new Spec("A null target gives nothing")
                .WhenAsync(NrunIsHandledWithoutTarget)
                .Then(SavingsBookCountShouldBe_, 0)
                .ExecuteAsync();
        }

        private void RunnerShouldBe_(NrunRunnerType expected) => Assert.AreEqual(expected, _handler.Runner);

        private static NrunPacket Packet => new()
        {
            Runner = NrunRunnerType.GetSavingsBook,
            VisualType = VisualType.Npc,
            VisualId = 1,
        };

        private Task NrunIsHandledWithAnNpc() =>
            _handler.HandleAsync(_session, _session.WithNpcAt(1, 1, 1), Packet);

        private Task NrunIsHandledWithAPlayerAsTheNpc() => _handler.HandleAsync(_session, _session.Character, Packet);

        private Task NrunIsHandledWithoutTarget() => _handler.HandleAsync(_session, null, Packet);

        private void SavingsBookCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(SavingsBookVNum));
    }
}

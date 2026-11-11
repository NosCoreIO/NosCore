//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.BankService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bank;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using NosCore.GameObject.Services.NpcDialogService;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class CuarryBankSavingsBookHandlerTests
    {
        private TestWorld World;
        private const short BookVNum = 5836;
        private ClientSession _session;
        private CuarryBankSavingsBookHandler _handler;
        private InventoryItemInstance _item;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            _session.Character.Reputation = 0;
            _handler = new CuarryBankSavingsBookHandler(new BankService(), World.NpcDialogService);
        }

        [TestMethod]
        public async Task UnrelatedEffectIsIgnored()
        {
            await new Spec("An item without the savings book effect opens nothing")
                .Given(BookInInventoryWithEffect_, ItemEffectType.Teleport)
                .WhenAsync(UsingTheBook)
                .Then(NoBankWindowShouldOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SavingsBookShowsTheBalanceAndIsKept()
        {
            await new Spec("Reading the savings book shows the balance and keeps the book")
                .Given(BookInInventoryWithEffect_, ItemEffectType.CuarryBankSavingsBook)
                .And(CharacterHasBankGold_, 2_000_000L)
                .WhenAsync(UsingTheBook)
                .Then(SavingsBookWindowShouldShow_Units, 2_000L)
                .And(BalanceLineShouldShow_Units, 2_000L)
                .And(BookCountShouldBe_, 1)
                .And(TheBankShouldBeUsable)
                .ExecuteAsync();
        }

        private void TheBankShouldBeUsable() =>
            Assert.IsTrue(World.NpcDialogService.IsOpen(_session.Character, NpcDialogWindow.Bank));

        private void BookInInventoryWithEffect_(ItemEffectType effect)
        {
            var item = new Item { VNum = BookVNum, Type = NoscorePocketType.Main, Effect = effect };
            var inst = new ItemInstanceForTest(BookVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void CharacterHasBankGold_(long gold) => _session.Character.BankGold = gold;

        private Task UsingTheBook() =>
            _handler.Handle(new ItemUsedEvent(_session, _item, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
            }));

        private void NoBankWindowShouldOpen() =>
            Assert.IsFalse(_session.LastPackets.OfType<GbPacket>().Any());

        private void SavingsBookWindowShouldShow_Units(long units)
        {
            var gb = _session.LastPackets.OfType<GbPacket>().Single();
            Assert.AreEqual(BankActionType.OpenFromSavingsBook, gb.Type);
            Assert.AreEqual(units, gb.GoldBank);
        }

        private void BalanceLineShouldShow_Units(long units)
        {
            var balance = _session.LastPackets.OfType<SMemoi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.BalanceBank, balance.Message);
            Assert.AreEqual(units, balance.Game18NArguments[0]);
        }

        private void BookCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(BookVNum));

        private sealed class ItemInstanceForTest(short vnum) : ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

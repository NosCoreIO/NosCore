//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
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
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class RefinerHandlerTests
    {
        private const short RefinerVNum = 1036;
        private const short GillionStone = 1013;
        private const short CellaPowder = 1014;
        private const short SoulGem = 1015;
        private const short CompleteSoulGem = 1016;
        private const int CellaRolls = 6;
        private const int ExtraRolls = 10_000;

        private TestWorld World;
        private ClientSession _session;
        private RefinerHandler _handler;
        private InventoryItemInstance _refiner;
        private readonly Queue<int> _draws = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.ItemList.Single(i => i.VNum == GillionStone).Material = MaterialType.GillionStone;
            World.ItemList.Single(i => i.VNum == CellaPowder).Material = MaterialType.CellaPowder;
            World.ItemList.Add(new Item
            {
                Type = NoscorePocketType.Main, VNum = RefinerVNum, ItemType = ItemType.Main,
                Effect = ItemEffectType.Refiner, EffectValue = (int)RefinerType.SoulGem,
            });
            World.ItemList.Add(new Item { Type = NoscorePocketType.Etc, VNum = CompleteSoulGem });
            _session = await World.CreateSessionAsync();
            // The test backpack has two Main slots, which the refiner and the stone take.
            _session.Character.InventoryService.Expensions[NoscorePocketType.Main] = 2;

            var rolls = World.Freeze<Mock<IDao<RollGeneratedItemDto, short>>>();
            short id = 1;
            var rows = Enumerable.Range(5, CellaRolls)
                .Select(amount => Row(id++, 0, CellaPowder, (short)amount, 1))
                .Concat([Row(id++, 1, SoulGem, 1, 2700), Row(id++, 1, CompleteSoulGem, 1, 300), Row(id, 1, CellaPowder, 0, 7000)])
                .ToList();
            rolls.Setup(r => r.Where(It.IsAny<Expression<Func<RollGeneratedItemDto, bool>>>())).Returns(rows);
            World.Random.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(() => _draws.Dequeue());

            _handler = World.Create<RefinerHandler>();
            _refiner = InventoryItemInstance.Create(World.GenerateItemProvider().Create(RefinerVNum, 3), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(_refiner);
        }

        [TestMethod]
        public async Task RefiningUsesUpTheRefinerAndAStoneForCellaAndAnExtraItem()
        {
            await new Spec("Refining uses up the refiner and one Gillion Stone, and gives the drawn Cella and extra item")
                .Given(TheCharacterHolds_GillionStones, 4)
                .And(TheDrawsAre__, 2, 0)
                .WhenAsync(UsingTheRefiner)
                .Then(TheCharacterHolds__, RefinerVNum, 2)
                .And(TheCharacterHolds__, GillionStone, 3)
                .And(TheCharacterHolds__, CellaPowder, 7)
                .And(TheCharacterHolds__, SoulGem, 1)
                .And(TheCellaLineSays_, 7)
                .And(TheSpecialItemLineNames_, SoulGem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADrawOfNothingExtraGivesOnlyCella()
        {
            await new Spec("A refine whose extra draw lands on nothing gives Cella alone")
                .Given(TheCharacterHolds_GillionStones, 1)
                .And(TheDrawsAre__, 5, ExtraRolls - 1)
                .WhenAsync(UsingTheRefiner)
                .Then(TheCharacterHolds__, GillionStone, 0)
                .And(TheCharacterHolds__, CellaPowder, 10)
                .And(TheCharacterHolds__, SoulGem, 0)
                .And(TheCharacterHolds__, CompleteSoulGem, 0)
                .And(NoSpecialItemLineIsSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutAGillionStoneTheRefinerStays()
        {
            await new Spec("Without a Gillion Stone nothing is refined and the character is told so")
                .WhenAsync(UsingTheRefiner)
                .Then(TheCharacterHolds__, RefinerVNum, 3)
                .And(TheCharacterHolds__, CellaPowder, 0)
                .And(TheCharacterIsTold_, Game18NConstString.NotEnoughGillion)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutRoomForTheResultNothingIsUsedUp()
        {
            await new Spec("A refine whose result does not fit keeps the refiner and the stone")
                .Given(TheCharacterHolds_GillionStones, 1)
                .And(AFullEtcPocket)
                .And(TheDrawsAre__, 0, 0)
                .WhenAsync(UsingTheRefiner)
                .Then(TheCharacterHolds__, RefinerVNum, 3)
                .And(TheCharacterHolds__, GillionStone, 1)
                .And(TheCharacterIsTold_, Game18NConstString.NotEnoughSpace)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMainItemWithAnotherEffectIsNotARefiner()
        {
            await new Spec("A Main item with another effect is left to its own handler")
                .Given(TheCharacterHolds_GillionStones, 1)
                .And(TheRefinerHasAnotherEffect)
                .WhenAsync(UsingTheRefiner)
                .Then(TheCharacterHolds__, RefinerVNum, 3)
                .And(TheCharacterHolds__, GillionStone, 1)
                .ExecuteAsync();
        }

        private static RollGeneratedItemDto Row(short id, short group, short vnum, short amount, short probability) => new()
        {
            RollGeneratedItemId = id,
            OriginalItemVNum = RefinerVNum,
            RollGroup = group,
            ItemGeneratedVNum = vnum,
            ItemGeneratedAmount = amount,
            Probability = probability,
        };

        private void TheCharacterHolds_GillionStones(int amount) =>
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(GillionStone, (short)amount), _session.Character.CharacterId));

        private void TheDrawsAre__(int cella, int extra)
        {
            _draws.Enqueue(cella);
            _draws.Enqueue(extra);
        }

        private void AFullEtcPocket()
        {
            var inventory = _session.Character.InventoryService;
            var provider = World.GenerateItemProvider();
            var fullStack = World.WorldConfiguration.Value.MaxItemAmount;
            while (inventory.CountItemInAnPocket(NoscorePocketType.Etc) < inventory.GetMaxSlots(NoscorePocketType.Etc))
            {
                inventory.AddItemToPocket(InventoryItemInstance.Create(provider.Create(SoulGem, fullStack), _session.Character.CharacterId));
            }
        }

        private void TheRefinerHasAnotherEffect() => _refiner.ItemInstance.Item.Effect = ItemEffectType.ItemPack;

        private Task UsingTheRefiner() => _handler.Handle(new ItemUsedEvent(_session, _refiner, new UseItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = _refiner.Slot,
        }));

        private void TheCharacterHolds__(short vnum, int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(vnum));

        private void TheCellaLineSays_(int amount) =>
            Assert.AreEqual(amount, _session.LastPackets.OfType<SayiPacket>()
                .Single(p => p.Message == Game18NConstString.CellaPowderItemAcquired).Game18NArguments[0]);

        private void TheSpecialItemLineNames_(short vnum) =>
            Assert.AreEqual((int)vnum, _session.LastPackets.OfType<SayiPacket>()
                .Single(p => p.Message == Game18NConstString.SpecialItemAcquired).Game18NArguments[0]);

        private void NoSpecialItemLineIsSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.SpecialItemAcquired));

        private void TheCharacterIsTold_(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == message));
    }
}

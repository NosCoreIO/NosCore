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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Dto;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class ItemPackHandlerTests
    {
        private const short PackVNum = 1015;
        private const short SeedVNum = 1012;
        private const short CostumeVNum = 924;
        private TestWorld World;
        private ClientSession _session;
        private Mock<IDao<RollGeneratedItemDto, short>> _rolls;
        private ItemPackHandler _handler;
        private InventoryItemInstance _pack;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _rolls = World.Freeze<Mock<IDao<RollGeneratedItemDto, short>>>();
            _rolls.Setup(r => r.Where(It.IsAny<Expression<Func<RollGeneratedItemDto, bool>>>()))
                .Returns(new List<RollGeneratedItemDto>
                {
                    new() { RollGeneratedItemId = 1, OriginalItemVNum = PackVNum, ItemGeneratedVNum = SeedVNum, ItemGeneratedAmount = 3, Probability = 1 },
                    new() { RollGeneratedItemId = 2, OriginalItemVNum = PackVNum, ItemGeneratedVNum = CostumeVNum, ItemGeneratedAmount = 1, Probability = 1 },
                });
            _handler = World.Create<ItemPackHandler>();
            _pack = InventoryItemInstance.Create(World.GenerateItemProvider().Create(PackVNum, 2), _session.Character.CharacterId);
            _pack.ItemInstance.Item.Effect = ItemEffectType.ItemPack;
            _session.Character.InventoryService.AddItemToPocket(_pack);
        }

        [TestMethod]
        public async Task OpeningAPackGivesEveryItem()
        {
            await new Spec("Opening a pack gives every listed item, announces each and uses up one pack")
                .WhenAsync(OpeningThePack)
                .Then(TheCharacterHolds__, SeedVNum, 3)
                .And(TheCharacterHolds__, CostumeVNum, 1)
                .And(TheCharacterHolds__, PackVNum, 1)
                .And(ReceivedLinesShouldBe_, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutRoomForEverythingThePackStays()
        {
            await new Spec("A pack whose items don't all fit gives nothing and stays")
                .Given(AFullEquipmentPocket)
                .WhenAsync(OpeningThePack)
                .Then(TheCharacterHolds__, SeedVNum, 0)
                .And(TheCharacterHolds__, PackVNum, 2)
                .And(TheNotEnoughSpaceLineIsSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemWithAnotherEffectIsLeftToItsOwnHandler()
        {
            await new Spec("An item with another effect is not opened as a pack")
                .Given(ThePackHasAnotherEffect)
                .WhenAsync(OpeningThePack)
                .Then(TheCharacterHolds__, SeedVNum, 0)
                .And(TheCharacterHolds__, PackVNum, 2)
                .ExecuteAsync();
        }

        private void ThePackHasAnotherEffect() => _pack.ItemInstance.Item.Effect = ItemEffectType.BoxEffect;

        private void AFullEquipmentPocket()
        {
            var inventory = _session.Character.InventoryService;
            var provider = World.GenerateItemProvider();
            while (inventory.CountItemInAnPocket(NoscorePocketType.Equipment) < inventory.GetMaxSlots(NoscorePocketType.Equipment))
            {
                inventory.AddItemToPocket(InventoryItemInstance.Create(provider.Create(1, 1), _session.Character.CharacterId));
            }
        }

        private Task OpeningThePack() => _handler.Handle(new ItemUsedEvent(_session, _pack, new UseItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Etc,
            Slot = _pack.Slot,
        }));

        private void TheCharacterHolds__(short vnum, int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(vnum));

        private void ReceivedLinesShouldBe_(int count) =>
            Assert.AreEqual(count, _session.LastPackets.OfType<SayiPacket>().Count(p => p.Message == Game18NConstString.ReceivedThisItem));

        private void TheNotEnoughSpaceLineIsSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.NotEnoughSpace));
    }
}

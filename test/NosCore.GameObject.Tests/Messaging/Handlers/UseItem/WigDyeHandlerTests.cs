//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class WigDyeHandlerTests
    {
        private const short BombVNum = 2280;
        private const short WigVNum = 428;
        private const short HelmetVNum = 4334;
        private const int Colours = 15;
        private TestWorld World;
        private ClientSession _session;
        private WigDyeHandler _handler;
        private InventoryItemInstance _bomb;
        private InventoryItemInstance _hat;
        private int _rolledMax;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(1);
            var random = World.Freeze<Mock<IRandomProvider>>();
            random.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>()))
                .Callback<int, int>((_, max) => _rolledMax = max)
                .Returns(7);
            _handler = World.Create<WigDyeHandler>();
            var bomb = new Item { VNum = BombVNum, Type = NoscorePocketType.Etc, ItemType = ItemType.Magical, Effect = ItemEffectType.WigDye, EffectValue = Colours };
            _bomb = Place(bomb, NoscorePocketType.Etc, 0);
        }

        [TestMethod]
        public async Task AWornWigTakesARandomColour()
        {
            await new Spec("A dye bomb gives the worn wig one of its colours and is used up")
                .Given(WearingAWig)
                .WhenAsync(UsingTheBomb)
                .Then(TheHatDesignShouldBe_, (short)7)
                .And(TheColourShouldBeRolledAmong_, Colours)
                .And(TheMapShouldSeeHairColour_, (HairColorType)7)
                .And(BombsLeftShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutAWigTheBombIsKept()
        {
            await new Spec("A dye bomb with no wig on says so and is kept")
                .WhenAsync(UsingTheBomb)
                .Then(TheNotWearingMessageShouldBeSent)
                .And(BombsLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHatThatCannotBeDyedKeepsTheBomb()
        {
            await new Spec("A dye bomb on a hat with no colours leaves the hat and keeps the bomb")
                .Given(WearingAHelmet)
                .WhenAsync(UsingTheBomb)
                .Then(TheHatDesignShouldBe_, (short)0)
                .And(TheNotWearingMessageShouldBeSent)
                .And(BombsLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARiderCannotDye()
        {
            await new Spec("A dye bomb does nothing while riding")
                .Given(WearingAWig)
                .And(Riding)
                .WhenAsync(UsingTheBomb)
                .Then(TheHatDesignShouldBe_, (short)0)
                .And(BombsLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        private void WearingAWig() => _hat = Place(new Item { VNum = WigVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion, EquipmentSlot = EquipmentType.Hat, IsColored = true },
            NoscorePocketType.Wear, (short)EquipmentType.Hat);

        private void WearingAHelmet() => _hat = Place(new Item { VNum = HelmetVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion, EquipmentSlot = EquipmentType.Hat },
            NoscorePocketType.Wear, (short)EquipmentType.Hat);

        private void Riding() => _session.Character.IsVehicled = true;

        private InventoryItemInstance Place(Item item, NoscorePocketType pocket, short slot)
        {
            var instance = InventoryItemInstance.Create(new ItemInstanceForTest(item) { Amount = 1 }, _session.Character.CharacterId);
            instance.Slot = slot;
            instance.Type = pocket;
            _session.Character.InventoryService[instance.ItemInstanceId] = instance;
            return instance;
        }

        private Task UsingTheBomb() => _handler.Handle(new ItemUsedEvent(_session, _bomb, new UseItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Etc,
            Slot = 0,
            Mode = 1,
        }));

        private void TheHatDesignShouldBe_(short design) => Assert.AreEqual(design, _hat.ItemInstance.Design);

        private void TheMapShouldSeeHairColour_(HairColorType colour) =>
            Assert.AreEqual(colour, _session.Character.GenerateEq().Haircolor);

        private void TheColourShouldBeRolledAmong_(int colours) => Assert.AreEqual(colours, _rolledMax);

        private void BombsLeftShouldBe_(int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(BombVNum));

        private void TheNotWearingMessageShouldBeSent() =>
            Assert.AreEqual(Game18NConstString.YouAreNotWearingItem,
                _session.LastPackets.OfType<MsgiPacket>().Single().Message);

        private sealed class ItemInstanceForTest(Item item) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = item.VNum;
            public Item Item { get; set; } = item;
            public object Clone() => MemberwiseClone();
        }
    }
}

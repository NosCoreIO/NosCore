//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class BuffPotionHandlerTests
    {
        private TestWorld World;
        private const short AttackPotionVNum = 1246;

        private static readonly IReadOnlyList<BCardDto> PotionBCards = new[]
        {
            new BCardDto { ItemVNum = AttackPotionVNum, Type = 25, SubType = 11, FirstData = 400, SecondData = 116 }
        };

        private ClientSession _session;
        private Mock<IBuffService> _buffs;
        private BuffPotionHandler _handler;
        private InventoryItemInstance _potion;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _buffs = new Mock<IBuffService>();
            var catalog = new Mock<ICardCatalog>();
            catalog.Setup(c => c.GetItemBCards(AttackPotionVNum)).Returns(PotionBCards);
            _handler = new BuffPotionHandler(_buffs.Object, catalog.Object);
        }

        [TestMethod]
        public async Task DrinkingAPotionAppliesItsCardAndUsesItUp()
        {
            GivePotion(ItemEffectType.BuffPotions);

            await UseAsync();

            _buffs.Verify(b => b.InflictCardsAsync(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), PotionBCards),
                Times.Once);
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(AttackPotionVNum));
        }

        [TestMethod]
        public async Task ADeadCharacterKeepsThePotion()
        {
            GivePotion(ItemEffectType.BuffPotions);
            _session.Character.IsAlive = false;

            await UseAsync();

            _buffs.VerifyNoOtherCalls();
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(AttackPotionVNum));
        }

        [TestMethod]
        public async Task AnotherEffectIsLeftToItsOwnHandler()
        {
            GivePotion(ItemEffectType.SpeedBooster);

            await UseAsync();

            _buffs.VerifyNoOtherCalls();
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(AttackPotionVNum));
        }

        private void GivePotion(ItemEffectType effect)
        {
            var item = new Item
            {
                VNum = AttackPotionVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = effect,
                EffectValue = 1,
            };
            var instance = new ItemInstanceForTest(AttackPotionVNum) { Amount = 1, Item = item };
            _potion = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _potion.Slot = 0;
            _potion.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_potion.ItemInstanceId] = _potion;
        }

        private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _potion, new UseItemPacket
        {
            VisualType = NosCore.Shared.Enumerations.VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = 0,
            Mode = 1,
            Parameter = 0,
        }));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class BandageHandlerTests
    {
        private const short BandageVNum = 2087;
        private const short BleedingGroup = 0;
        private const short MinorBleeding = 1;
        private const short Bleeding = 21;
        private const short HeavyBleeding = 42;
        private const short Poison = 25;

        private static readonly BCardDto StopsBleedingsUpToLevelTwo = new()
        {
            ItemVNum = BandageVNum,
            Type = BCardEffect.BuffCancelGroupOfEffects.Type(),
            SubType = BCardEffect.BuffCancelGroupOfEffects.SubType(),
            FirstData = BleedingGroup,
            SecondData = 2,
        };

        private readonly Dictionary<short, CardDto> _cards = new()
        {
            [MinorBleeding] = new CardDto { CardId = MinorBleeding, BuffType = BuffType.Bad, Level = 1, GroupId = BleedingGroup, Duration = 600 },
            [Bleeding] = new CardDto { CardId = Bleeding, BuffType = BuffType.Bad, Level = 2, GroupId = BleedingGroup, Duration = 600 },
            [HeavyBleeding] = new CardDto { CardId = HeavyBleeding, BuffType = BuffType.Bad, Level = 3, GroupId = BleedingGroup, Duration = 600 },
            [Poison] = new CardDto { CardId = Poison, BuffType = BuffType.Bad, Level = 1, GroupId = 5, Duration = 600 },
        };

        private TestWorld World;
        private ClientSession _session;
        private IBuffService _buffs;
        private InventoryItemInstance _bandage;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var catalog = World.Freeze<Mock<ICardCatalog>>();
            catalog.Setup(c => c.GetCard(It.IsAny<short>())).Returns((short id) => _cards.GetValueOrDefault(id));
            catalog.Setup(c => c.GetCardBCards(It.IsAny<short>())).Returns([]);
            catalog.Setup(c => c.GetItemBCards(BandageVNum)).Returns([StopsBleedingsUpToLevelTwo]);
            World.UseReal<IBuffService, BuffService>();
            _buffs = World.Create<IBuffService>();
        }

        [TestMethod]
        public async Task ABandageStopsTheBleedingsUpToItsLevelAndIsUsedUp()
        {
            await new Spec("A bandage stops the bleedings up to its level, leaves heavier ones and other debuffs, and is used up")
                .GivenAsync(TheCharacterSuffers_, new[] { MinorBleeding, Bleeding, HeavyBleeding, Poison })
                .And(TheCharacterHas_Bandages, 2)
                .WhenAsync(UsingABandage)
                .Then(TheActiveCardsShouldBe_, new[] { Poison, HeavyBleeding })
                .And(TheCharacterShouldHave_Bandages, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABandageWithNothingToStopIsStillUsedUp()
        {
            await new Spec("A bandage used with no bleeding is used up all the same")
                .Given(TheCharacterHas_Bandages, 1)
                .WhenAsync(UsingABandage)
                .Then(TheCharacterShouldHave_Bandages, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDeadKeepTheirBandage()
        {
            await new Spec("A dead character cannot use a bandage")
                .GivenAsync(TheCharacterSuffers_, new[] { MinorBleeding })
                .And(TheCharacterHas_Bandages, 1)
                .And(TheCharacterIsDead)
                .WhenAsync(UsingABandage)
                .Then(TheActiveCardsShouldBe_, new[] { MinorBleeding })
                .And(TheCharacterShouldHave_Bandages, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemWithAnotherEffectIsLeftAlone()
        {
            await new Spec("An item that is not a bandage is left to its own handler")
                .GivenAsync(TheCharacterSuffers_, new[] { MinorBleeding })
                .And(TheCharacterHas_ItemsWithEffect_, 1, ItemEffectType.NoEffect)
                .WhenAsync(UsingABandage)
                .Then(TheActiveCardsShouldBe_, new[] { MinorBleeding })
                .And(TheCharacterShouldHave_Bandages, 1)
                .ExecuteAsync();
        }

        private async Task TheCharacterSuffers_(short[] cards)
        {
            foreach (var card in cards)
            {
                await _buffs.ApplyAsync(_session.Character, _cards[card], [], null);
            }
        }

        private void TheCharacterHas_Bandages(int amount) => TheCharacterHas_ItemsWithEffect_(amount, ItemEffectType.Bandage);

        private void TheCharacterHas_ItemsWithEffect_(int amount, ItemEffectType effect)
        {
            var item = new Item { VNum = BandageVNum, Type = NoscorePocketType.Etc, ItemType = ItemType.Magical, Effect = effect };
            var instance = new ItemInstanceForTest(BandageVNum) { Amount = (short)amount, Item = item };
            _bandage = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _bandage.Slot = 0;
            _bandage.Type = NoscorePocketType.Etc;
            _session.Character.InventoryService[_bandage.ItemInstanceId] = _bandage;
        }

        private void TheCharacterIsDead() => _session.Character.IsAlive = false;

        private Task UsingABandage() => World.Create<BandageHandler>().Handle(new ItemUsedEvent(_session, _bandage, new UseItemPacket
        {
            VisualType = NosCore.Shared.Enumerations.VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Etc,
            Slot = 0,
            Mode = 1,
            Parameter = 0,
        }));

        private void TheActiveCardsShouldBe_(short[] expected) =>
            CollectionAssert.AreEquivalent(expected, _buffs.GetActiveBuffs(_session.Character).Select(b => b.CardId).ToArray());

        private void TheCharacterShouldHave_Bandages(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(BandageVNum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Etc };
            public object Clone() => MemberwiseClone();
        }
    }
}

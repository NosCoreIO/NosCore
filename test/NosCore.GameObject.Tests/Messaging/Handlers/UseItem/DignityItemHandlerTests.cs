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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class DignityItemHandlerTests
    {
        private const short DignityItemVNum = 2156;
        private ClientSession _session = null!;
        private DignityItemHandler _handler = null!;
        private InventoryItemInstance _dignityItem = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            DignityLevels.Load(ClientLadders.DignityLevels());
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new DignityItemHandler();
        }

        [TestMethod]
        public async Task ADignityItemRaisesDignityByItsValueAndIsConsumed()
        {
            await new Spec("A dignity item below the maximum adds its EffectValue and is used up")
                .Given(ADignityItemRestoring_, 100)
                .And(CharacterHasDignity_, (short)-150)
                .WhenAsync(UsingTheItem)
                .Then(DignityShouldBe_, (short)-50)
                .And(ItemCountShouldBe_, 0)
                .And(DignityIncreasedBy_ShouldBeAnnounced, 100)
                .And(TheReputationBarShouldShowDignity_, (short)-50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheIncreaseStopsAtTheMaximum()
        {
            await new Spec("A value larger than the room left fills dignity to the maximum and announces only what was added")
                .Given(ADignityItemRestoring_, 2000)
                .And(CharacterHasDignity_, (short)40)
                .WhenAsync(UsingTheItem)
                .Then(DignityShouldBe_, (short)100)
                .And(ItemCountShouldBe_, 0)
                .And(DignityIncreasedBy_ShouldBeAnnounced, 60)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AtTheMaximumTheItemIsRefusedAndKept()
        {
            await new Spec("At the maximum the item is not used up and the client is told dignity is maxed")
                .Given(ADignityItemRestoring_, 100)
                .And(CharacterHasDignity_, (short)100)
                .WhenAsync(UsingTheItem)
                .Then(DignityShouldBe_, (short)100)
                .And(ItemCountShouldBe_, 1)
                .And(DignityIsAtMaximumShouldBeShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutAnImportedMaximumTheItemIsKept()
        {
            await new Spec("A ladder imported without a maximum leaves the item and dignity untouched")
                .Given(ADignityItemRestoring_, 100)
                .And(TheLadderHasNoMaximum)
                .And(CharacterHasDignity_, (short)-150)
                .WhenAsync(UsingTheItem)
                .Then(DignityShouldBe_, (short)-150)
                .And(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemWithAnotherEffectIsIgnored()
        {
            await new Spec("A magical item with a different effect is left to its own handler")
                .Given(AMagicalItemWithEffect_, ItemEffectType.Teleport)
                .And(CharacterHasDignity_, (short)-150)
                .WhenAsync(UsingTheItem)
                .Then(DignityShouldBe_, (short)-150)
                .And(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSameEffectOnANonMagicalItemIsIgnored()
        {
            await new Spec("Effect 14 only restores dignity on magical items")
                .Given(A_ItemWithTheDignityEffect, ItemType.Special)
                .And(CharacterHasDignity_, (short)-150)
                .WhenAsync(UsingTheItem)
                .Then(DignityShouldBe_, (short)-150)
                .And(ItemCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void ADignityItemRestoring_(int value) => GiveItem(ItemType.Magical, ItemEffectType.RestoreDignity, value);

        private void AMagicalItemWithEffect_(ItemEffectType effect) => GiveItem(ItemType.Magical, effect, 100);

        private void A_ItemWithTheDignityEffect(ItemType itemType) => GiveItem(itemType, ItemEffectType.RestoreDignity, 100);

        private void GiveItem(ItemType itemType, ItemEffectType effect, int value)
        {
            var item = new Item
            {
                VNum = DignityItemVNum,
                Type = NoscorePocketType.Main,
                ItemType = itemType,
                Effect = effect,
                EffectValue = value,
            };
            var instance = new ItemInstanceForTest(DignityItemVNum) { Amount = 1, Item = item };
            _dignityItem = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _dignityItem.Slot = 0;
            _dignityItem.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_dignityItem.ItemInstanceId] = _dignityItem;
        }

        private void TheLadderHasNoMaximum() => DignityLevels.Load(new List<DignityLevelDto>
        {
            new() { DignityLevelId = (byte)DignityType.Default, MaxDignity = null }
        });

        private void CharacterHasDignity_(short dignity) => _session.Character.Dignity = dignity;

        private async Task UsingTheItem()
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _dignityItem, packet));
        }

        private void DignityShouldBe_(short expected) => Assert.AreEqual(expected, _session.Character.Dignity);

        private void ItemCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(DignityItemVNum));

        private void DignityIncreasedBy_ShouldBeAnnounced(int amount)
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .SingleOrDefault(p => p.Message == Game18NConstString.DignityIncreased);
            Assert.IsNotNull(say);
            Assert.AreEqual(SayColorType.Green, say.Type);
            Assert.AreEqual(amount, Convert.ToInt32(say.Game18NArguments.Single()));
        }

        private void TheReputationBarShouldShowDignity_(short dignity)
        {
            var fd = _session.LastPackets.OfType<FdPacket>().LastOrDefault();
            Assert.IsNotNull(fd);
            Assert.AreEqual(dignity, fd.Dignity);
        }

        private void DignityIsAtMaximumShouldBeShown() =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>()
                .Any(p => p.Message == Game18NConstString.DignityMaximum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

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
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using Moq;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class SpecialistSkinHandlerTests
    {
        private const short MasterchefSkinVNum = 9227;
        private const short MasterchefLook = 37;
        private const short ChefCardVNum = 4575;
        private const short ChefMorph = 38;
        private const short AnglerCardVNum = 4562;
        private const short AnglerMorph = 35;

        private TestWorld World;
        private ClientSession _session;
        private SpecialistSkinHandler _handler;
        private InventoryItemInstance _skin;
        private SpecialistInstance _card;
        private Mock<ISessionGroup> _onlookers;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _onlookers = new Mock<ISessionGroup>();
            _session.Character.MapInstance.Sessions = _onlookers.Object;
            _handler = World.Create<SpecialistSkinHandler>();
            SkinInInventory();
        }

        [TestMethod]
        public async Task TheSkinGoesOnTheMatchingCard()
        {
            await new Spec("Using the Masterchef skin with the Chef card worn puts the skin on that card and uses it up")
                .Given(TheCard_WithMorph_IsWorn, ChefCardVNum, ChefMorph)
                .WhenAsync(UsingTheSkin)
                .Then(TheCardShouldWear_, MasterchefLook)
                .And(SkinCountShouldBe_, 0)
                .And(NoCModeIsSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATransformedPlayerTakesTheSkinsLookAtOnce()
        {
            await new Spec("Transformed with the Chef card, the skin replaces the look the map sees")
                .Given(TheCard_WithMorph_IsWorn, ChefCardVNum, ChefMorph)
                .And(CharacterIsTransformedAs_, ChefMorph)
                .WhenAsync(UsingTheSkin)
                .Then(CharacterMorphShouldBe_, MasterchefLook)
                .And(TheMapIsSentTheMorph_, MasterchefLook)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherCardIsRefused()
        {
            await new Spec("The Masterchef skin does not fit the Angler card")
                .Given(TheCard_WithMorph_IsWorn, AnglerCardVNum, AnglerMorph)
                .WhenAsync(UsingTheSkin)
                .Then(Message_ShouldBeSent, Game18NConstString.SkinNotCompatibleCard)
                .And(TheCardShouldWear_, (short)0)
                .And(SkinCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACardMustBeWorn()
        {
            await new Spec("Without a worn specialist card the skin is kept")
                .WhenAsync(UsingTheSkin)
                .Then(Message_ShouldBeSent, Game18NConstString.NoSpecialistCardEquipped)
                .And(SkinCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSameSkinIsNotUsedTwice()
        {
            await new Spec("A card already wearing the skin keeps the second one in the bag")
                .Given(TheCard_WithMorph_IsWorn, ChefCardVNum, ChefMorph)
                .And(TheCardAlreadyWearsTheSkin)
                .WhenAsync(UsingTheSkin)
                .Then(Message_ShouldBeSent, Game18NConstString.AlreadyWearingSameMask)
                .And(SkinCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void SkinInInventory()
        {
            var item = new Item
            {
                VNum = MasterchefSkinVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = ItemEffectType.SpecialistSkin,
                EffectValue = ChefMorph,
                Morph = MasterchefLook,
            };
            var inst = new ItemInstanceForTest(MasterchefSkinVNum) { Amount = 1, Item = item };
            _skin = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _skin.Slot = 0;
            _skin.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_skin.ItemInstanceId] = _skin;
        }

        private void TheCard_WithMorph_IsWorn(short vnum, short morph)
        {
            _card = new SpecialistInstance(new Item
            {
                VNum = vnum,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp,
                Morph = morph,
            });
            var worn = InventoryItemInstance.Create(_card, _session.Character.CharacterId);
            worn.Type = NoscorePocketType.Wear;
            worn.Slot = (byte)EquipmentType.Sp;
            _session.Character.InventoryService[worn.ItemInstanceId] = worn;
        }

        private void TheCardAlreadyWearsTheSkin() => _card.Skin = MasterchefLook;

        private void CharacterIsTransformedAs_(short morph)
        {
            _session.Character.UseSp = true;
            _session.Character.Morph = morph;
        }

        private async Task UsingTheSkin()
        {
            await _handler.Handle(new ItemUsedEvent(_session, _skin, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 0,
                Parameter = 0,
            }));
        }

        private void TheCardShouldWear_(short look) => Assert.AreEqual(look, _card.Skin);

        private void CharacterMorphShouldBe_(short morph) => Assert.AreEqual(morph, _session.Character.Morph);

        private void TheMapIsSentTheMorph_(short morph)
        {
            var cMode = Broadcast().OfType<CModePacket>().Single();
            Assert.AreEqual(_session.Character.CharacterId, cMode.VisualId);
            Assert.AreEqual(morph, cMode.Morph);
        }

        private void NoCModeIsSent() => Assert.IsFalse(Broadcast().OfType<CModePacket>().Any());

        private IEnumerable<IPacket> Broadcast() =>
            _onlookers.Invocations.SelectMany(i => i.Arguments)
                .SelectMany(a => a is IEnumerable<IPacket> many ? many : a is IPacket one ? new[] { one } : []);

        private void SkinCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(MasterchefSkinVNum));

        private void Message_ShouldBeSent(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == message));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

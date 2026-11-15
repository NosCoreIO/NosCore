//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class ReleaseScrollHandlerTests
    {
        private TestWorld World;
        private const short WeaponSlot = 2;
        private ClientSession _session;
        private ReleaseScrollHandler _handler;
        private InventoryItemInstance _scroll;
        private WearableInstance _weapon;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _handler = World.Create<ReleaseScrollHandler>();
        }

        [TestMethod]
        public async Task DroppingTheScrollOnAFixedItemAsksFirst()
        {
            PlaceScroll();
            PlaceWeapon(isFixed: true);

            await UseAsync(mode: 0, PocketType.Equipment, WeaponSlot);

            var question = _session.LastPackets.OfType<QnaiPacket>().Single();
            Assert.AreEqual(Game18NConstString.AskWantToUse, question.Question);
            var yes = ((UseItemPacket)question.YesPacket.NotNull()).NotNull();
            Assert.AreEqual(1, yes.Mode);
            Assert.AreEqual(PocketType.Equipment, yes.TargetType);
            Assert.AreEqual(WeaponSlot, yes.TargetSlot);
            Assert.IsTrue(_weapon.IsFixed);
            Assert.AreEqual(1, ScrollsLeft());
        }

        [TestMethod]
        public async Task ConfirmingReleasesTheItemAndUsesUpTheScroll()
        {
            PlaceScroll();
            PlaceWeapon(isFixed: true);

            await UseAsync(mode: 1, PocketType.Equipment, WeaponSlot);

            Assert.IsFalse(_weapon.IsFixed);
            Assert.AreEqual(0, ScrollsLeft());
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(m => m.Message == Game18NConstString.CanModifyEquipmentLevel));
        }

        [TestMethod]
        public async Task AnItemThatIsNotFixedKeepsTheScroll()
        {
            PlaceScroll();
            PlaceWeapon(isFixed: false);

            await UseAsync(mode: 1, PocketType.Equipment, WeaponSlot);

            Assert.AreEqual(1, ScrollsLeft());
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(m => m.Message == Game18NConstString.UseOnlyOnFixEquipment));
        }

        [TestMethod]
        public async Task DoubleClickingTheScrollAloneDoesNothing()
        {
            PlaceScroll();
            PlaceWeapon(isFixed: true);

            await UseAsync(mode: 1, null, null);

            Assert.IsTrue(_weapon.IsFixed);
            Assert.AreEqual(1, ScrollsLeft());
        }

        [TestMethod]
        public async Task AnItemWithoutTheReleaseEffectDoesNothing()
        {
            PlaceScroll(ItemEffectType.NoEffect);
            PlaceWeapon(isFixed: true);

            await UseAsync(mode: 1, PocketType.Equipment, WeaponSlot);

            Assert.IsTrue(_weapon.IsFixed);
            Assert.AreEqual(1, ScrollsLeft());
        }

        private void PlaceScroll(ItemEffectType effect = ItemEffectType.ReleaseScroll)
        {
            var scroll = new ItemInstance(new Item { VNum = 1219, Type = NoscorePocketType.Main, ItemType = ItemType.Upgrade, Effect = effect })
            {
                Amount = 1,
            };
            _scroll = InventoryItemInstance.Create(scroll, _session.Character.CharacterId);
            _scroll.Slot = 0;
            _scroll.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_scroll.ItemInstanceId] = _scroll;
        }

        private void PlaceWeapon(bool isFixed)
        {
            _weapon = new WearableInstance(new Item
            {
                VNum = 1, Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon,
                EquipmentSlot = EquipmentType.MainWeapon,
            }, new Mock<ILogger<WearableInstance>>().Object, World.LogLanguageLocalizer)
            {
                Amount = 1,
                Upgrade = 7,
                IsFixed = isFixed,
            };
            var inst = InventoryItemInstance.Create(_weapon, _session.Character.CharacterId);
            inst.Slot = WeaponSlot;
            inst.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private int ScrollsLeft() => _session.Character.InventoryService.CountItem(_scroll.ItemInstance.ItemVNum);

        private Task UseAsync(short mode, PocketType? targetType, short? targetSlot) =>
            _handler.Handle(new ItemUsedEvent(_session, _scroll, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = mode,
                Parameter = 1,
                TargetType = targetType,
                TargetSlot = targetSlot,
            }));
    }
}

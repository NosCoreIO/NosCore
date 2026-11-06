//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class SoulRevivalHandlerTests
    {
        private const short CardSlot = 2;
        private ClientSession _session = null!;
        private SoulRevivalHandler _handler = null!;
        private InventoryItemInstance _stone = null!;
        private SpecialistInstance _card = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            TestHelpers.Instance.WorldConfiguration.Value.MaxSpPoints = 10000;
            _handler = new SoulRevivalHandler(TestHelpers.Instance.WorldConfiguration);
        }

        [TestMethod]
        public async Task DroppingTheStoneOnADestroyedCardAsksFirst()
        {
            PlaceStone(SoulRevivalHandler.SoulRevivalStoneVNum);
            PlaceCard(SpUpgradeOperationBase.DestroyedSoulRare);

            await UseAsync(mode: 0, PocketType.Equipment, CardSlot);

            var question = _session.LastPackets.OfType<QnaiPacket>().Single();
            Assert.AreEqual(Game18NConstString.AskWantToUse, question.Question);
            var yes = (UseItemPacket)question.YesPacket!;
            Assert.AreEqual(1, yes.Mode);
            Assert.AreEqual(PocketType.Equipment, yes.TargetType);
            Assert.AreEqual(CardSlot, yes.TargetSlot);
            Assert.AreEqual(SpUpgradeOperationBase.DestroyedSoulRare, _card.Rare);
            Assert.AreEqual(1, StonesLeft());
        }

        [TestMethod]
        public async Task ConfirmingRevivesTheCardAndRestoresSpPoints()
        {
            PlaceStone(SoulRevivalHandler.EventSoulRevivalStoneVNum);
            PlaceCard(SpUpgradeOperationBase.DestroyedSoulRare);
            _session.Character.SpPoint = 0;

            await UseAsync(mode: 1, PocketType.Equipment, CardSlot);

            Assert.AreEqual(0, _card.Rare);
            Assert.AreEqual(0, StonesLeft());
            Assert.AreEqual(10000, _session.Character.SpPoint);
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == Game18NConstString.CardSoulRevived));
        }

        [TestMethod]
        public async Task ACardWithItsSoulIsLeftAlone()
        {
            PlaceStone(SoulRevivalHandler.SoulRevivalStoneVNum);
            PlaceCard(0);

            await UseAsync(mode: 1, PocketType.Equipment, CardSlot);

            Assert.AreEqual(1, StonesLeft());
            Assert.IsFalse(_session.LastPackets.OfType<MsgiPacket>().Any());
        }

        [TestMethod]
        public async Task DoubleClickingTheStoneAloneDoesNothing()
        {
            PlaceStone(SoulRevivalHandler.SoulRevivalStoneVNum);
            PlaceCard(SpUpgradeOperationBase.DestroyedSoulRare);

            await UseAsync(mode: 1, null, null);

            Assert.AreEqual(SpUpgradeOperationBase.DestroyedSoulRare, _card.Rare);
            Assert.AreEqual(1, StonesLeft());
        }

        private void PlaceStone(short vnum)
        {
            var stone = new ItemInstance(new Item { VNum = vnum, Type = NoscorePocketType.Main, ItemType = ItemType.Upgrade })
            {
                Amount = 1,
            };
            _stone = InventoryItemInstance.Create(stone, _session.Character.CharacterId);
            _stone.Slot = 0;
            _stone.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_stone.ItemInstanceId] = _stone;
        }

        private void PlaceCard(sbyte rare)
        {
            _card = new SpecialistInstance(new Item
            {
                VNum = 902, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp,
            })
            {
                Amount = 1,
                Rare = rare,
                Upgrade = 7,
            };
            var inst = InventoryItemInstance.Create(_card, _session.Character.CharacterId);
            inst.Slot = CardSlot;
            inst.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private int StonesLeft() => _session.Character.InventoryService.CountItem(_stone.ItemInstance.ItemVNum);

        private Task UseAsync(short mode, PocketType? targetType, short? targetSlot) =>
            _handler.Handle(new ItemUsedEvent(_session, _stone, new UseItemPacket
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

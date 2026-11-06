//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

#pragma warning disable CS0618
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class StartupInventoryTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        private InvPacket Pocket(PocketType type) =>
            _session.Character.GenerateInv(NullLogger.Instance, TestHelpers.Instance.LogLanguageLocalizer)
                .OfType<InvPacket>().Single(p => p.Type == type);

        [TestMethod]
        public void ASpecialistCardInTheBagIsListedInTheEquipmentPocket()
        {
            var card = new SpecialistInstance(new Item
            {
                VNum = 901, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist, EquipmentSlot = EquipmentType.Sp
            })
            {
                Amount = 1,
                Upgrade = 7,
                SpStoneUpgrade = 3,
            };
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(card, _session.Character.CharacterId), NoscorePocketType.Equipment, 1);

            var listed = Pocket(PocketType.Equipment).IvnSubPackets!.Single();

            Assert.AreEqual((short)901, listed.VNum);
            Assert.AreEqual((short)1, listed.Slot);
            Assert.AreEqual((short)7, listed.UpgradeDesign);
            Assert.AreEqual((byte)3, listed.SecondUpgrade);
            Assert.IsFalse(Pocket(PocketType.Costume).IvnSubPackets!.Any());
        }

        [TestMethod]
        public void ASpecialistCardMovedIntoTheBagKeepsItsPerfection()
        {
            var card = new SpecialistInstance(new Item { VNum = 901, ItemType = ItemType.Specialist, EquipmentSlot = EquipmentType.Sp })
            {
                SpStoneUpgrade = 3,
            };

            Assert.AreEqual((byte)3, card.GenerateIvnSubPacket(PocketType.Equipment, 4).SecondUpgrade);
        }
    }
}

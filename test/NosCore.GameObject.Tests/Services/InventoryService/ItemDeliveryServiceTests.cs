//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class ItemDeliveryServiceTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IMailHub> _mail;
        private ItemDeliveryService _delivery;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _mail = World.Freeze<Mock<IMailHub>>();
            _delivery = World.Create<ItemDeliveryService>();
        }

        [TestMethod]
        public async Task AnItemReturnedWhileItsSlotIsTakenIsShownInBothSlots()
        {
            var source = Holds(1012, 1);
            Assert.IsTrue(_session.Character.InventoryService.TryConsume(source, 1));
            var taker = Holds(1013, 1);
            _session.LastPackets.Clear();

            await _delivery.ReturnConsumedAsync(_session.Character, source, 1);

            var refreshed = _session.LastPackets.OfType<IvnPacket>().SelectMany(p => p.IvnSubPackets ?? []).ToList();
            Assert.AreEqual(taker.Slot, refreshed[0].Slot);
            Assert.AreEqual((short)1013, refreshed[0].VNum);
            Assert.AreEqual(source.Slot, refreshed[1].Slot);
            Assert.AreEqual((short)1012, refreshed[1].VNum);
            Assert.AreNotEqual(taker.Slot, source.Slot);
        }

        [TestMethod]
        public async Task AnItemReturnedToAFullInventoryIsSentToTheParcelBox()
        {
            var source = Holds(1012, 1);
            Assert.IsTrue(_session.Character.InventoryService.TryConsume(source, 1));
            var inventory = _session.Character.InventoryService;
            while (inventory.CountItemInAnPocket(NoscorePocketType.Main) < inventory.GetMaxSlots(NoscorePocketType.Main))
            {
                Holds(1013, 999);
            }

            await _delivery.ReturnConsumedAsync(_session.Character, source, 1);

            _mail.Verify(m => m.SendMailAsync(It.Is<MailRequest>(r => r.VNum == 1012 && r.Amount == 1)), Times.Once);
        }

        private InventoryItemInstance Holds(short vnum, short amount)
        {
            var entry = InventoryItemInstance.Create(World.GenerateItemProvider().Create(vnum, amount), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(entry);
            return entry;
        }
    }
}

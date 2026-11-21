//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Interfaces;
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
        private Mock<ILogger<ItemDeliveryService>> _logger;
        private MailRequest? _sent;
        private ItemDeliveryService _delivery;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _mail = World.Freeze<Mock<IMailHub>>();
            _mail.Setup(m => m.SendMailAsync(It.IsAny<MailRequest>()))
                .Callback<MailRequest>(r => _sent = r).ReturnsAsync(true);
            _logger = World.Freeze<Mock<ILogger<ItemDeliveryService>>>();
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
            var source = ConsumedFromAFullBag(1012, 1);

            await _delivery.ReturnConsumedAsync(_session.Character, source, 1);

            var parcel = await ParcelAsync();
            Assert.AreEqual((short)1012, parcel.ItemVNum);
            Assert.AreEqual((short)1, parcel.Amount);
        }

        [TestMethod]
        public async Task AFailedParcelSendKeepsTheItemAndLogsIt()
        {
            _mail.Setup(m => m.SendMailAsync(It.IsAny<MailRequest>())).ReturnsAsync(false);
            var source = ConsumedFromAFullBag(1012, 1);

            await _delivery.ReturnConsumedAsync(_session.Character, source, 1);

            Assert.AreEqual(1, Kept(1012));
            ErrorWasLogged();
        }

        [TestMethod]
        public async Task AParcelSendThatThrowsKeepsTheItemAndLogsIt()
        {
            _mail.Setup(m => m.SendMailAsync(It.IsAny<MailRequest>())).ThrowsAsync(new TimeoutException());
            var source = ConsumedFromAFullBag(1012, 1);

            await _delivery.ReturnConsumedAsync(_session.Character, source, 1);

            Assert.AreEqual(1, Kept(1012));
            ErrorWasLogged();
        }

        [TestMethod]
        public async Task AnAmountReturnedToAFullStackNeverGoesAboveTheStackLimit()
        {
            var source = Holds(1012, 999);
            Assert.IsTrue(_session.Character.InventoryService.TryConsume(source, 100));
            Holds(1012, 100);

            await _delivery.ReturnConsumedAsync(_session.Character, source, 100);

            Assert.IsTrue(_session.Character.InventoryService.Values.All(i => i.ItemInstance.Amount <= 999));
            Assert.AreEqual(1099, _session.Character.InventoryService.CountItem(1012));
        }

        [TestMethod]
        public async Task WhatAFullStackCannotTakeBackGoesToTheParcelBox()
        {
            var source = Holds(1012, 999);
            Assert.IsTrue(_session.Character.InventoryService.TryConsume(source, 100));
            Holds(1012, 100);
            FillMain();

            await _delivery.ReturnConsumedAsync(_session.Character, source, 100);

            Assert.AreEqual(999, source.ItemInstance.Amount);
            Assert.AreEqual((short)100, (await ParcelAsync()).Amount);
        }

        [TestMethod]
        public async Task AReturnWhoseRefreshFailsStillReachesTheParcelBox()
        {
            var source = ConsumedFromAFullBag(1012, 1);
            var sender = new Mock<IPacketSender>();
            sender.Setup(s => s.SendPacketAsync(It.IsAny<IPacket?>())).ThrowsAsync(new TimeoutException());
            sender.Setup(s => s.SendPacketsAsync(It.IsAny<IEnumerable<IPacket?>>())).ThrowsAsync(new TimeoutException());
            _session.Character.Sender = sender.Object;

            await Assert.ThrowsExactlyAsync<TimeoutException>(() => _delivery.ReturnConsumedAsync(_session.Character, source, 1));

            Assert.AreEqual((short)1012, (await ParcelAsync()).ItemVNum);
        }

        [TestMethod]
        public async Task AParcelThatCannotBeStoredIsLoggedAsLost()
        {
            var items = World.Freeze<Mock<IDao<IItemInstanceDto?, Guid>>>();
            items.Setup(d => d.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>())).ReturnsAsync((IItemInstanceDto?)null);
            var delivery = World.Create<ItemDeliveryService>();
            var source = ConsumedFromAFullBag(1012, 1);

            await delivery.ReturnConsumedAsync(_session.Character, source, 1);

            _mail.Verify(m => m.SendMailAsync(It.IsAny<MailRequest>()), Times.Never);
            _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => $"{state}".Contains("PARCEL_NOT_STORED")),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        private InventoryItemInstance ConsumedFromAFullBag(short vnum, short amount)
        {
            var source = Holds(vnum, amount);
            Assert.IsTrue(_session.Character.InventoryService.TryConsume(source, amount));
            FillMain();
            return source;
        }

        private void FillMain()
        {
            var inventory = _session.Character.InventoryService;
            while (inventory.CountItemInAnPocket(NoscorePocketType.Main) < inventory.GetMaxSlots(NoscorePocketType.Main))
            {
                Holds(1013, 999);
            }
        }

        private async Task<IItemInstanceDto> ParcelAsync()
        {
            var id = _sent?.Mail.ItemInstanceId.NotNull();
            return (await World.ItemInstanceDao.FirstOrDefaultAsync(i => i.Id == id)).NotNull();
        }

        private int Kept(short vnum) =>
            (World.ItemInstanceDao.Where(i => i.ItemVNum == vnum) ?? []).Count();

        private void ErrorWasLogged() =>
            _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);

        private InventoryItemInstance Holds(short vnum, short amount)
        {
            var entry = InventoryItemInstance.Create(World.GenerateItemProvider().Create(vnum, amount), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(entry);
            return entry;
        }
    }
}

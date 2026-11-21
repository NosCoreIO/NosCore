//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Moq;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MailService;
using NosCore.PacketHandlers.Parcel;
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Parcel
{
    // An item given back to a full bag goes through the real parcel box and is taken out again.
    [TestClass]
    public class ParcelReturnTests
    {
        private const short WeaponVNum = 9901;
        private const short BeadVNum = 9902;
        private const short SeedVNum = 1012;
        private const short OtherMainVNum = 1013;
        private TestWorld World;
        private ClientSession _session;
        private ParcelRegistry _parcels;
        private ItemDeliveryService _delivery;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.ItemList.Add(new Item { VNum = WeaponVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon });
            World.ItemList.Add(new Item { VNum = BeadVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Box });
            World.Inject<IItemCatalog>(new ItemCatalog(World.ItemList));
            _session = await World.CreateSessionAsync();
            World.ChannelHub.Setup(c => c.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo>());
            World.Freeze<Mock<IPubSubHub>>().Setup(p => p.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            _parcels = World.Create<ParcelRegistry>();
            World.Inject<IParcelRegistry>(_parcels);
            World.Inject<IMailHub>(new MailHub(World.Create<MailService>()));
            _delivery = World.Create<ItemDeliveryService>();
        }

        [TestMethod]
        public async Task AWeaponReturnedToAFullBagComesBackWithItsOptions()
        {
            var weapon = Holds(WeaponVNum);
            Assert.IsNotNull(await World.ItemInstanceDao.TryInsertOrUpdateAsync(weapon.ItemInstance));
            Assert.IsNotNull(await World.EquipmentOptionDao.TryInsertOrUpdateAsync(new EquipmentOptionDto
            {
                Id = Guid.NewGuid(), WearableInstanceId = weapon.ItemInstanceId, Type = 3, Level = 2, Value = 40
            }));

            var back = await ReturnedToAFullBagAndTakenOutAsync(weapon);

            var options = World.EquipmentOptionDao.Where(o => o.WearableInstanceId == back.ItemInstanceId)?.ToList() ?? [];
            Assert.AreEqual((short)WeaponVNum, back.ItemInstance.ItemVNum);
            Assert.AreEqual((3, 2, 40), options.Select(o => ((int)o.Type, (int)o.Level, o.Value)).Single());
        }

        [TestMethod]
        public async Task ABeadReturnedToAFullBagKeepsWhatItHolds()
        {
            var bead = Holds(BeadVNum);
            var holder = (BoxInstance)bead.ItemInstance;
            holder.HoldingVNum = 333;
            holder.SpLevel = 7;

            var back = await ReturnedToAFullBagAndTakenOutAsync(bead);

            var held = back.ItemInstance as BoxInstance;
            Assert.AreEqual((short?)333, held?.HoldingVNum);
            Assert.AreEqual((byte?)7, held?.SpLevel);
        }

        [TestMethod]
        public async Task ASlotTakenAfterThePlacementLeavesTheWeaponInTheBag()
        {
            var mail = await AWeaponWaitingInTheParcelBoxAsync();
            // Another writer takes a slot while the parcel is being claimed, after the weapon was placed.
            var hub = ParcelBox(onDelete: () => Holds(BeadVNum));

            await ClaimAsync(hub, mail);

            Assert.AreEqual(1, WeaponsInTheBag());
            Assert.IsFalse(IsInTheParcelBox(mail));
        }

        [TestMethod]
        public async Task ASlotTakenBeforeThePlacementLeavesTheParcelWhereItIs()
        {
            var mail = await AWeaponWaitingInTheParcelBoxAsync();
            var hub = ParcelBox(onRead: () => Holds(BeadVNum));

            await ClaimAsync(hub, mail);

            Assert.AreEqual(0, WeaponsInTheBag());
            Assert.IsTrue(IsInTheParcelBox(mail));
        }

        [TestMethod]
        public async Task OptionsThatCannotBeWrittenLeaveTheParcelWhereItIs()
        {
            var mail = await AWeaponWaitingInTheParcelBoxAsync();
            var options = World.Freeze<Mock<IDao<EquipmentOptionDto, Guid>>>();
            options.Setup(o => o.Where(It.IsAny<System.Linq.Expressions.Expression<Func<EquipmentOptionDto, bool>>>()))
                .Returns([new EquipmentOptionDto { Id = Guid.NewGuid(), Type = 3, Level = 2, Value = 40 }]);
            options.Setup(o => o.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<EquipmentOptionDto>>()))
                .ThrowsAsync(new TimeoutException());

            await Assert.ThrowsExactlyAsync<TimeoutException>(() => ClaimAsync(ParcelBox(), mail));

            Assert.AreEqual(0, WeaponsInTheBag());
            Assert.IsTrue(IsInTheParcelBox(mail));
        }

        [TestMethod]
        public async Task ARemovalThatFailsLeavesTheParcelWhereItIs()
        {
            var mail = await AWeaponWaitingInTheParcelBoxAsync();
            var hub = ParcelBox(deleteThrows: true);

            await Assert.ThrowsExactlyAsync<TimeoutException>(() => ClaimAsync(hub, mail));

            Assert.AreEqual(0, WeaponsInTheBag());
            Assert.IsTrue(IsInTheParcelBox(mail));
        }

        [TestMethod]
        public async Task ARemovalThatThrowsAfterRemovingLeavesTheWeaponInTheBag()
        {
            var mail = await AWeaponWaitingInTheParcelBoxAsync();
            var hub = ParcelBox(deleteThrows: true, deleteRemoves: true);

            await ClaimAsync(hub, mail);

            Assert.AreEqual(1, WeaponsInTheBag());
            Assert.IsFalse(IsInTheParcelBox(mail));
        }

        [TestMethod]
        public async Task AWeaponUsedBeforeAFailedClaimCouldBeUndoneStillTakesTheParcelAway()
        {
            var mail = await AWeaponWaitingInTheParcelBoxAsync();
            var hub = ParcelBox(onDelete: UseTheWeapon, deleteThrows: true, failingDeletes: 1);

            await ClaimAsync(hub, mail);

            Assert.AreEqual(0, WeaponsInTheBag() + (IsInTheParcelBox(mail) ? 1 : 0), "the used weapon is not in the parcel box too");
        }

        [TestMethod]
        public async Task AParcelThatCannotBeRemovedAfterItsWeaponWasUsedIsLogged()
        {
            var mail = await AWeaponWaitingInTheParcelBoxAsync();
            var logger = World.Freeze<Mock<ILogger<PclPacketHandler>>>();
            var hub = ParcelBox(onDelete: UseTheWeapon, deleteThrows: true);

            await ClaimAsync(hub, mail);

            logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => $"{state}".Contains("PARCEL_CLAIMED_BUT_KEPT")),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [TestMethod]
        public async Task ASeedUsedBeforeAFailedClaimCouldBeUndoneLeavesTheRestWithThePlayer()
        {
            var inventory = _session.Character.InventoryService;
            var seeds = Holds(SeedVNum, 10);
            Assert.IsTrue(inventory.TryConsume(seeds, 10));
            while (inventory.CountItemInAnPocket(NoscorePocketType.Main) < inventory.GetMaxSlots(NoscorePocketType.Main))
            {
                Holds(OtherMainVNum, 999);
            }

            await _delivery.ReturnConsumedAsync(_session.Character, seeds, 10);
            inventory.DeleteFromTypeAndSlot(NoscorePocketType.Main, 0);
            var mail = _parcels.GetMails(_session.Character.CharacterId, false).Values.Single();
            // The player uses one seed while the claim is still waiting on the parcel box.
            var hub = ParcelBox(onDelete: () => inventory.TryConsume(inventory.Values.Single(i => i.ItemInstance.ItemVNum == SeedVNum), 1),
                deleteThrows: true, failingDeletes: 1);

            await ClaimAsync(hub, mail);

            Assert.AreEqual(9, inventory.CountItem(SeedVNum));
            Assert.IsFalse(IsInTheParcelBox(mail));
        }

        // The player sells or wears the weapon while the claim is still waiting on the parcel box.
        private void UseTheWeapon() =>
            _session.Character.InventoryService.DeleteById(_session.Character.InventoryService.Values
                .Single(i => i.ItemInstance.ItemVNum == WeaponVNum).ItemInstanceId);

        private async Task<MailData> AWeaponWaitingInTheParcelBoxAsync()
        {
            var inventory = _session.Character.InventoryService;
            var weapon = Holds(WeaponVNum);
            Assert.IsTrue(inventory.TryConsume(weapon, 1));
            while (inventory.CountItemInAnPocket(NoscorePocketType.Equipment) < inventory.GetMaxSlots(NoscorePocketType.Equipment))
            {
                Holds(BeadVNum);
            }

            await _delivery.ReturnConsumedAsync(_session.Character, weapon, 1);
            inventory.DeleteFromTypeAndSlot(NoscorePocketType.Equipment, 0);
            return _parcels.GetMails(_session.Character.CharacterId, false).Values.Single();
        }

        // The real parcel box, with a hook on its reads and a scripted removal.
        private IMailHub ParcelBox(Action? onRead = null, Action? onDelete = null, bool deleteThrows = false,
            bool deleteRemoves = false, int failingDeletes = int.MaxValue)
        {
            var hub = new Mock<IMailHub>();
            hub.Setup(m => m.GetMails(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync((long id, long character, bool copy) =>
                {
                    onRead?.Invoke();
                    onRead = null;
                    return _parcels.GetMails(character, copy).Values.Where(m => m.MailId == id).ToList();
                });
            hub.Setup(m => m.DeleteMailAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync((long id, long character, bool copy) =>
                {
                    onDelete?.Invoke();
                    onDelete = null;
                    var fails = deleteThrows && failingDeletes-- > 0;
                    var removed = (!fails || deleteRemoves) && _parcels.RemoveMail(character, copy, id, out _);
                    return fails ? throw new TimeoutException() : removed;
                });
            return hub.Object;
        }

        private Task ClaimAsync(IMailHub hub, MailData mail)
        {
            World.Inject(hub);
            return World.Create<PclPacketHandler>().ExecuteAsync(new PclPacket { Type = 4, GiftId = (int)mail.MailId }, _session);
        }

        private int WeaponsInTheBag() =>
            _session.Character.InventoryService.Values.Count(i => i.ItemInstance.ItemVNum == WeaponVNum);

        private bool IsInTheParcelBox(MailData mail) =>
            _parcels.GetMails(_session.Character.CharacterId, false).ContainsKey(mail.MailId);

        private async Task<InventoryItemInstance> ReturnedToAFullBagAndTakenOutAsync(InventoryItemInstance source)
        {
            var inventory = _session.Character.InventoryService;
            Assert.IsTrue(inventory.TryConsume(source, 1));
            while (inventory.CountItemInAnPocket(NoscorePocketType.Equipment) < inventory.GetMaxSlots(NoscorePocketType.Equipment))
            {
                Holds(WeaponVNum);
            }

            await _delivery.ReturnConsumedAsync(_session.Character, source, 1);
            inventory.DeleteFromTypeAndSlot(NoscorePocketType.Equipment, 0);
            var mail = _parcels.GetMails(_session.Character.CharacterId, false).Values.Single();
            await World.Create<PclPacketHandler>().ExecuteAsync(new PclPacket { Type = 4, GiftId = (int)mail.MailId }, _session);
            return inventory.LoadBySlotAndType(0, NoscorePocketType.Equipment).NotNull();
        }

        private InventoryItemInstance Holds(short vnum, short amount = 1)
        {
            var entry = InventoryItemInstance.Create(World.GenerateItemProvider().Create(vnum, amount), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(entry);
            return entry;
        }
    }
}

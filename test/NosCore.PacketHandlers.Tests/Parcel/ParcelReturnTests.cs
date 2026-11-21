//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
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

        private InventoryItemInstance Holds(short vnum)
        {
            var entry = InventoryItemInstance.Create(World.GenerateItemProvider().Create(vnum, 1), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(entry);
            return entry;
        }
    }
}

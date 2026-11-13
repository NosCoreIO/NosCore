//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Warehouse;
using NosCore.Tests.Shared;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class CarriedWarehouseHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            World.Freeze<Mock<IWarehouseHub>>()
                .Setup(s => s.GetWarehouseItems(null, It.IsAny<long?>(), It.IsAny<WarehouseType>(), null))
                .ReturnsAsync(new List<WarehouseLink>());
            World.UseReal<IWarehouseTransferService, WarehouseTransferService>();
            World.UseReal<IPersonalWarehouseService, PersonalWarehouseService>();
        }

        private void Has(StaticBonusType bonus) =>
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = _session.Character.CharacterId,
                StaticBonusType = bonus
            });

        private Task UsingAsync(GuriPacketType type) =>
            World.Create<CarriedWarehouseHandler>().Handle(new GuriPacketReceivedEvent(_session, new ClientGuriPacket { Type = type }));

        [TestMethod]
        public async Task ThePetBasketOpensTheWarehouse()
        {
            Has(StaticBonusType.PetBasket);

            await UsingAsync(GuriPacketType.PetBasket);

            Assert.AreEqual(1, _session.LastPackets.OfType<StashAllPacket>().Count());
        }

        [TestMethod]
        public async Task WithoutAPetBasketTheWarehouseStaysClosed()
        {
            await UsingAsync(GuriPacketType.PetBasket);

            Assert.AreEqual(0, _session.LastPackets.Count);
        }

        [TestMethod]
        public async Task ThePartnerBackpackOpensItsOwnStorage()
        {
            Has(StaticBonusType.PetBackPack);

            await UsingAsync(GuriPacketType.PartnerBackpack);

            Assert.AreEqual(50, _session.LastPackets.OfType<PStashAllPacket>().Single().WarehouseSize);
        }

        [TestMethod]
        public async Task TheNosMatePanelShowsOnlyTheStoragesBought()
        {
            Has(StaticBonusType.PetBackPack);

            await World.Create<IPersonalWarehouseService>().SendCarriedStoragesAsync(_session);

            var ib = _session.LastPackets.OfType<IbPacket>().Single();
            Assert.AreEqual(CarriedStorageType.PartnerBackpack, ib.Storage);
            Assert.IsTrue(ib.IsAvailable);
        }
    }
}

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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class ArenaOfMastersTicketHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private ArenaOfMastersTicketHandler _handler;
        private InventoryItemInstance _ticket;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _handler = World.Create<ArenaOfMastersTicketHandler>();
        }

        [TestMethod]
        public async Task UsingATicketAddsAnEntryAndUsesItUp()
        {
            PlaceTicket(ItemEffectType.ArenaOfMastersTicket, amount: 2);
            _session.Character.CharacterDto.MasterTicket = 3;

            await UseAsync();

            Assert.AreEqual(4, _session.Character.CharacterDto.MasterTicket);
            Assert.AreEqual(1, TicketsLeft());
            PacketAssert.AreEqual(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.ArenaMastersEntriesIncreased,
            }, _session.LastPackets.OfType<MsgiPacket>().Single());
        }

        [TestMethod]
        public async Task AnItemWithoutTheTicketEffectDoesNothing()
        {
            PlaceTicket(ItemEffectType.NoEffect, amount: 1);

            await UseAsync();

            Assert.AreEqual(0, _session.Character.CharacterDto.MasterTicket);
            Assert.AreEqual(1, TicketsLeft());
            Assert.IsFalse(_session.LastPackets.OfType<MsgiPacket>().Any());
        }

        private void PlaceTicket(ItemEffectType effect, short amount)
        {
            var ticket = new ItemInstance(new Item { VNum = 1, Type = NoscorePocketType.Main, ItemType = ItemType.Special, Effect = effect })
            {
                Amount = amount,
            };
            _ticket = InventoryItemInstance.Create(ticket, _session.Character.CharacterId);
            _ticket.Slot = 0;
            _ticket.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_ticket.ItemInstanceId] = _ticket;
        }

        private int TicketsLeft() => _session.Character.InventoryService.CountItem(_ticket.ItemInstance.ItemVNum);

        private Task UseAsync() =>
            _handler.Handle(new ItemUsedEvent(_session, _ticket, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
            }));
    }
}

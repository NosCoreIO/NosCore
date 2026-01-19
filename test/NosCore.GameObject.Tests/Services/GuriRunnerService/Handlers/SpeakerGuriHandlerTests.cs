//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.GuriRunnerService.Handlers;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Services.GuriRunnerService.Handlers
{
    [TestClass]
    public class SpeakerGuriHandlerTests : GuriEventHandlerTestsBase
    {
        private IItemGenerationService? ItemProvider;
        private Mock<ILogger>? Logger;
        private ClientSession? ReceiverSession;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Magical, Type =  NoscorePocketType.Etc, Effect = ItemEffectType.Speaker},
            };
            Logger = new Mock<ILogger>();
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger.Object, TestHelpers.Instance.LogLanguageLocalizer);

            Session = await TestHelpers.Instance.GenerateSessionAsync();
            ReceiverSession = await TestHelpers.Instance.GenerateSessionAsync();

            Handler = new SpeakerGuriHandler(Logger.Object, TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.GameLanguageLocalizer, TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task TestSpeakerWithItemAsync()
        {
            Session!.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1, 1), 0));
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = 0,
                Data = 999,
                Value = "2 0 {test}"
            });
            var sayitempacket = (SayItemPacket?)ReceiverSession!.LastPackets.FirstOrDefault(s => s is SayItemPacket);
            Assert.IsNotNull(sayitempacket);
            var saypacket = (SayPacket?)ReceiverSession!.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsNull(saypacket);
        }


        [TestMethod]
        public async Task TestSpeakerWithItemDoesNotExistAsync()
        {
            Session!.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1, 1), 0));
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = 0,
                Data = 999,
                Value = "2 1 {test}"
            });
            var sayitempacket = (SayItemPacket?)ReceiverSession!.LastPackets.FirstOrDefault(s => s is SayItemPacket);
            Assert.IsNull(sayitempacket);
            var saypacket = (SayPacket?)ReceiverSession!.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsNull(saypacket);
        }


        [TestMethod]
        public async Task TestSpeakerWithoutItemAsync()
        {
            Session!.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider!.Create(1, 1), 0));
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = 0,
            });
            var sayitempacket = (SayItemPacket?)ReceiverSession!.LastPackets.FirstOrDefault(s => s is SayItemPacket);
            Assert.IsNull(sayitempacket);
            var saypacket = (SayPacket?)ReceiverSession!.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsNotNull(saypacket);
        }

        [TestMethod]
        public async Task TestSpeakerWithNoSpeakerAsync()
        {
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = 0
            });
            var sayitempacket = (SayItemPacket?)ReceiverSession!.LastPackets.FirstOrDefault(s => s is SayItemPacket);
            Assert.IsNull(sayitempacket);
            var saypacket = (SayPacket?)ReceiverSession!.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsNull(saypacket);
        }
    }
}

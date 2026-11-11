//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Algorithm.DignityService;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Threading.Tasks;
using NosCore.Tests.Shared.BDD.Steps;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.ShopService;
using System.Linq;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class ShoppingPacketHandlerTests
    {
        private TestWorld World;
        private ShoppingPacketHandler Handler;
        private ClientSession Session;
        private Mock<IDignityService> DignityService;
        private readonly ILogger<ShoppingPacketHandler> Logger = new Mock<ILogger<ShoppingPacketHandler>>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            DignityService = World.Freeze<Mock<IDignityService>>();
            DignityService.Setup(x => x.GetLevelFromDignity(It.IsAny<short>())).Returns(DignityType.Default);

            Handler = World.Create<ShoppingPacketHandler>();
        }

        [TestMethod]
        public async Task ShoppingFromUnknownVisualTypeShouldBeIgnored()
        {
            await new Spec("Shopping from unknown visual type should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoppingFromUnknownVisualType)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShoppingFromNonExistentNpcShouldBeIgnored()
        {
            await new Spec("Shopping from non existent npc should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoppingFromNonExistentNpc)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShoppingFromNonExistentPlayerShouldBeIgnored()
        {
            await new Spec("Shopping from non existent player should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoppingFromNonExistentPlayer)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShoppingWithoutTalkingToTheNpcIsIgnored()
        {
            await new Spec("Shopping at an NPC whose dialog was never opened sends no shop")
                .Given(AShopNpcStandsNextToTheCharacter)
                .WhenAsync(ShoppingFromTheShopNpc)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShoppingAfterTalkingToTheNpcSendsTheShop()
        {
            await new Spec("Shopping at an NPC the character is talking to sends its shop")
                .Given(AShopNpcStandsNextToTheCharacter)
                .And(TheNpcsDialogIsOpen)
                .WhenAsync(ShoppingFromTheShopNpc)
                .Then(TheShopShouldBeSent)
                .ExecuteAsync();
        }

        private void AShopNpcStandsNextToTheCharacter()
        {
            var npc = Session.WithNpcAt(100, 1, 1);
            var shopItems = new System.Collections.Concurrent.ConcurrentDictionary<int, ShopItem>();
            shopItems[0] = new ShopItem { Slot = 0, Price = 100, ItemInstance = World.GenerateItemProvider().Create(1012, 1) };
            npc.Shop = new Shop { ShopItems = shopItems };
        }

        private void TheNpcsDialogIsOpen() => Session.WithWindowOpenedAt(100, NpcDialogWindow.Conversation);

        private Task ShoppingFromTheShopNpc() =>
            Handler.ExecuteAsync(new ShoppingPacket { VisualType = VisualType.Npc, VisualId = 100 }, Session);

        private void TheShopShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.Any(p => p is NosCore.Packets.ServerPackets.Shop.NInvPacket));

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private async Task ShoppingFromUnknownVisualType()
        {
            await Handler.ExecuteAsync(new ShoppingPacket
            {
                VisualType = (VisualType)99,
                VisualId = 1,
                ShopType = 0
            }, Session);
        }

        private async Task ShoppingFromNonExistentNpc()
        {
            await Handler.ExecuteAsync(new ShoppingPacket
            {
                VisualType = VisualType.Npc,
                VisualId = 99999,
                ShopType = 0
            }, Session);
        }

        private async Task ShoppingFromNonExistentPlayer()
        {
            await Handler.ExecuteAsync(new ShoppingPacket
            {
                VisualType = VisualType.Player,
                VisualId = 99999,
                ShopType = 0
            }, Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

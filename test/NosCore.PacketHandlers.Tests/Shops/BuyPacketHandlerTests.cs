//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.DignityService;
using NosCore.GameObject.Ecs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.ShopService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD.Steps;
using Microsoft.Extensions.Logging;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.SkillService;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class BuyPacketHandlerTests
    {
        private TestWorld World;
        private BuyPacketHandler Handler;
        private const int ShopNpcId = 100;
        private ClientSession Session;
        private ClientSession Seller;
        private readonly Mock<ISkillService> SkillService = new();
        private readonly ILogger<BuyPacketHandler> Logger = new Mock<ILogger<BuyPacketHandler>>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Handler = new BuyPacketHandler(
                Logger,
                World.LogLanguageLocalizer,
                World.SessionRegistry,
                World.WorldConfiguration,
                World.GenerateItemProvider(), World.NpcDialogService,
                new Mock<NosCore.GameObject.Services.ShellOptionService.IShellOptionService>().Object,
                new Mock<NosCore.Dao.Interfaces.IDao<NosCore.Data.Dto.IItemInstanceDto?, System.Guid>>().Object,
                new Mock<IFamilyShopService>().Object,
                World.Create<DignityService>(),
                SkillService.Object);
        }

        [TestMethod]
        public async Task BuyingFromUnknownVisualTypeShouldBeIgnored()
        {
            await new Spec("Buying from unknown visual type should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(BuyingFromUnknownVisualType)
                .Then(NoErrorPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromNonExistentNpcShouldBeIgnored()
        {
            await new Spec("Buying from non existent npc should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(BuyingFromNonExistentNpc)
                .Then(NoErrorPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromNonExistentPlayerShouldBeIgnored()
        {
            await new Spec("Buying from non existent player should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(BuyingFromNonExistentPlayer)
                .Then(NoErrorPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromAnOpenNpcShopNearbySucceeds()
        {
            await new Spec("Buying from the NPC shop the player opened and still stands at takes the price")
                .Given(AShopNpcStandsAt_, (short)2, (short)2)
                .And(CharacterHasGold_, 1_000L)
                .And(TheShopWindowWasOpenedAtTheNpc)
                .WhenAsync(BuyingFromTheNpc)
                .Then(GoldShouldBe_, 900L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingASkillFromAnOpenSkillShopTeachesIt()
        {
            await new Spec("A purchase without an amount at a skill shop teaches the skill it names")
                .Given(ASkillTeacherStandsNearby)
                .And(TheShopWindowWasOpenedAtTheNpc)
                .WhenAsync(BuyingSkill_, (short)20)
                .Then(SkillShouldBeTaught_, (short)20)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingASkillTheShopDoesNotTeachIsIgnored()
        {
            await new Spec("A purchase of a skill the shop does not list teaches nothing")
                .Given(ASkillTeacherStandsNearby)
                .And(TheShopWindowWasOpenedAtTheNpc)
                .WhenAsync(BuyingSkill_, (short)21)
                .Then(NoSkillShouldBeTaught)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingWithoutOpeningTheNpcShopIsIgnored()
        {
            await new Spec("Buying from an NPC shop that was never opened does nothing")
                .Given(AShopNpcStandsAt_, (short)2, (short)2)
                .And(CharacterHasGold_, 1_000L)
                .WhenAsync(BuyingFromTheNpc)
                .Then(GoldShouldBe_, 1_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingAfterWalkingAwayFromTheNpcShopIsIgnored()
        {
            await new Spec("Buying from an NPC shop the player has walked away from does nothing")
                .Given(AShopNpcStandsAt_, (short)2, (short)2)
                .And(CharacterHasGold_, 1_000L)
                .And(TheShopWindowWasOpenedAtTheNpc)
                .And(CharacterWalksTo_, (short)30, (short)30)
                .WhenAsync(BuyingFromTheNpc)
                .Then(GoldShouldBe_, 1_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromANearbyPlayerShopSucceeds()
        {
            await new Spec("Buying from a player shop opened next to its owner takes the price")
                .GivenAsync(APlayerShopStandsAt_, (short)1, (short)1)
                .And(CharacterHasGold_, 1_000L)
                .And(TheShopWindowWasOpenedAtThePlayer)
                .WhenAsync(BuyingFromThePlayer)
                .Then(GoldShouldBe_, 900L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromAPlayerShopOnAnotherMapIsIgnored()
        {
            await new Spec("Buying from a player shop on another map does nothing")
                .GivenAsync(APlayerShopStandsAt_, (short)1, (short)1)
                .And(CharacterHasGold_, 1_000L)
                .And(TheShopWindowWasOpenedAtThePlayer)
                .And(TheSellerIsOnAnotherMap)
                .WhenAsync(BuyingFromThePlayer)
                .Then(GoldShouldBe_, 1_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromAPlayerShopWithoutOpeningItIsIgnored()
        {
            await new Spec("Buying from a player shop that was never opened does nothing")
                .GivenAsync(APlayerShopStandsAt_, (short)1, (short)1)
                .And(CharacterHasGold_, 1_000L)
                .WhenAsync(BuyingFromThePlayer)
                .Then(GoldShouldBe_, 1_000L)
                .ExecuteAsync();
        }

        private Shop AShop()
        {
            var shopItems = new ConcurrentDictionary<int, ShopItem>();
            shopItems[0] = new ShopItem
            {
                Slot = 0,
                Price = 100,
                ItemInstance = World.GenerateItemProvider().Create(1012, 1)
            };
            return new Shop { ShopItems = shopItems };
        }

        private void AShopNpcStandsAt_(short x, short y)
        {
            var npc = Session.WithNpcAt(ShopNpcId, x, y);
            npc.Shop = AShop();
        }

        private async Task APlayerShopStandsAt_(short x, short y)
        {
            Seller = await World.CreateSessionAsync();
            Seller.WalkTo(x, y);
            Seller.Character.Shop = AShop();
            Seller.IsRegistered();
        }

        private void CharacterHasGold_(long gold) => Session.Character.Gold = gold;

        private void CharacterWalksTo_(short x, short y) => Session.WalkTo(x, y);

        private void TheShopWindowWasOpenedAtTheNpc() => Session.WithWindowOpenedAt(ShopNpcId, NpcDialogWindow.Shop);

        private void TheShopWindowWasOpenedAtThePlayer() =>
            World.NpcDialogService.Open(Session.Character, Seller.Character, NpcDialogWindow.Shop);

        private void TheSellerIsOnAnotherMap() => Seller.Character.MapInstanceId = Guid.NewGuid();

        private Task BuyingFromTheNpc() => Handler.ExecuteAsync(new BuyPacket
        {
            VisualType = VisualType.Npc,
            VisualId = ShopNpcId,
            Slot = 0,
            Amount = 1
        }, Session);

        private Task BuyingFromThePlayer() => Handler.ExecuteAsync(new BuyPacket
        {
            VisualType = VisualType.Player,
            VisualId = Seller.Character.VisualId,
            Slot = 0,
            Amount = 1
        }, Session);

        private void ASkillTeacherStandsNearby()
        {
            var npc = Session.WithNpcAt(ShopNpcId, 2, 2);
            npc.Shop = new Shop
            {
                ShopSkills = [new ShopSkill { SkillVNum = 20, Skill = new SkillDto { SkillVNum = 20 } }]
            };
        }

        private Task BuyingSkill_(short skillVNum) => Handler.ExecuteAsync(new BuyPacket
        {
            VisualType = VisualType.Npc,
            VisualId = ShopNpcId,
            Slot = skillVNum
        }, Session);

        private void SkillShouldBeTaught_(short skillVNum)
        {
            var character = Session.Character;
            SkillService.Verify(s => s.BuyAsync(character, It.Is<SkillDto>(k => k.SkillVNum == skillVNum)), Times.Once);
        }

        private void NoSkillShouldBeTaught() =>
            SkillService.Verify(s => s.BuyAsync(It.IsAny<NosCore.GameObject.Ecs.PlayerComponentBundle>(), It.IsAny<SkillDto>()), Times.Never);

        private void GoldShouldBe_(long gold) => Assert.AreEqual(gold, Session.Character.Gold);

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private async Task BuyingFromUnknownVisualType()
        {
            await Handler.ExecuteAsync(new BuyPacket
            {
                VisualType = (VisualType)99,
                VisualId = 1,
                Slot = 0,
                Amount = 1
            }, Session);
        }

        private async Task BuyingFromNonExistentNpc()
        {
            await Handler.ExecuteAsync(new BuyPacket
            {
                VisualType = VisualType.Npc,
                VisualId = 99999,
                Slot = 0,
                Amount = 1
            }, Session);
        }

        private async Task BuyingFromNonExistentPlayer()
        {
            await Handler.ExecuteAsync(new BuyPacket
            {
                VisualType = VisualType.Player,
                VisualId = 99999,
                Slot = 0,
                Amount = 1
            }, Session);
        }

        private void NoErrorPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class SoundFlowerHandlerTests
    {
        private const short SoundFlowerVNum = 1087;

        private TestWorld World;
        private ClientSession Session;
        private Mock<ISoundFlowerService> SoundFlowers;
        private InventoryItemInstance Flower;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            SoundFlowers = World.Freeze<Mock<ISoundFlowerService>>();
        }

        [TestMethod]
        public async Task AFlowerInTheBagStartsAFlowerQuestAndIsUsedUp()
        {
            await new Spec("A Sound Flower already in a bag starts a flower quest and is used up")
                .Given(TheCharacterCarriesAnItemWith_, ItemEffectType.SoundFlower)
                .And(TheQuestCanBeTaken_, true)
                .WhenAsync(TheItemIsUsed)
                .Then(AFlowerQuestIsAskedFor_Times, 1)
                .And(TheCharacterHas_FlowersLeft, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFlowerWhoseQuestIsRefusedIsKept()
        {
            await new Spec("A flower whose quest cannot be taken stays in the bag")
                .Given(TheCharacterCarriesAnItemWith_, ItemEffectType.SoundFlower)
                .And(TheQuestCanBeTaken_, false)
                .WhenAsync(TheItemIsUsed)
                .Then(TheCharacterHas_FlowersLeft, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherItemStartsNoQuest()
        {
            await new Spec("An item without the Sound Flower effect starts no quest")
                .Given(TheCharacterCarriesAnItemWith_, ItemEffectType.NoEffect)
                .And(TheQuestCanBeTaken_, true)
                .WhenAsync(TheItemIsUsed)
                .Then(AFlowerQuestIsAskedFor_Times, 0)
                .And(TheCharacterHas_FlowersLeft, 1)
                .ExecuteAsync();
        }

        private void TheCharacterCarriesAnItemWith_(ItemEffectType effect)
        {
            var flower = new ItemInstance(new Item
            {
                VNum = SoundFlowerVNum, Type = NoscorePocketType.Main, ItemType = ItemType.Map, Effect = effect,
            })
            {
                Amount = 1,
            };
            Flower = InventoryItemInstance.Create(flower, Session.Character.CharacterId);
            Flower.Slot = 0;
            Flower.Type = NoscorePocketType.Main;
            Session.Character.InventoryService[Flower.ItemInstanceId] = Flower;
        }

        private void TheQuestCanBeTaken_(bool taken) =>
            SoundFlowers.Setup(s => s.StartFlowerQuestAsync(It.IsAny<PlayerComponentBundle>())).ReturnsAsync(taken);

        private Task TheItemIsUsed() =>
            World.Create<SoundFlowerHandler>().Handle(new ItemUsedEvent(Session, Flower, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
            }));

        private void AFlowerQuestIsAskedFor_Times(int times) =>
            SoundFlowers.Verify(s => s.StartFlowerQuestAsync(It.IsAny<PlayerComponentBundle>()), Times.Exactly(times));

        private void TheCharacterHas_FlowersLeft(int left) =>
            Assert.AreEqual(left, Session.Character.InventoryService.CountItem(SoundFlowerVNum));
    }
}

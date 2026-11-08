//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.QuestService.Handlers;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Quest
{
    [TestClass]
    public class ItemQuestProgressHandlerTests
    {
        private const short QuestItem = 1012;
        private const short OtherItem = 1013;
        private const short Npc = 300;
        private const short OtherNpc = 301;

        private ClientSession _session = null!;
        private IItemGenerationService _itemProvider = null!;
        private Mock<IQuestService> _questService = null!;
        private ItemQuestProgressHandler _handler = null!;
        private CharacterQuest _quest = null!;
        private QuestObjectiveDto _objective = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();
            _itemProvider = TestHelpers.Instance.GenerateItemProvider();
            _questService = new Mock<IQuestService>();
            _handler = new ItemQuestProgressHandler(_questService.Object,
                new IQuestTypeHandler[]
                {
                    new GoToQuestHandler(),
                    new CollectQuestHandler(),
                    new NeededQuestHandler(),
                    new BringsQuestHandler(),
                    new RequiredQuestHandler(),
                    new ProductQuestHandler(),
                    new UseQuestHandler(),
                    new WearQuestHandler()
                },
                NullLogger<ItemQuestProgressHandler>.Instance);
        }

        [TestMethod]
        public async Task CollectingCountsTheCarriedItems()
        {
            await new Spec("Picking up part of a collect quest's items moves its counter without completing it")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Collect, 5, QuestItem)
                .And(TheCharacterCarries_Of_, 2, QuestItem)
                .WhenAsync(TheCharacterReceives_, QuestItem)
                .Then(TheObjectiveShouldRead_, 2)
                .And(TheProgressShouldBeSentToTheClient)
                .And(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CollectingTheLastItemCompletesTheQuestAndKeepsTheItems()
        {
            await new Spec("Carrying every item of a collect quest completes it and the items stay in the bag")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Collect, 5, QuestItem)
                .And(TheCharacterCarries_Of_, 5, QuestItem)
                .WhenAsync(TheCharacterReceives_, QuestItem)
                .Then(TheObjectiveShouldRead_, 5)
                .And(TheQuestShouldBeCompleted)
                .And(TheCharacterShouldStillCarry_Of_, 5, QuestItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CollectingAnotherItemIsIgnored()
        {
            await new Spec("Picking up an item a collect quest does not ask for changes nothing")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Collect, 5, QuestItem)
                .And(TheCharacterCarries_Of_, 5, OtherItem)
                .WhenAsync(TheCharacterReceives_, OtherItem)
                .Then(TheObjectiveShouldRead_, 0)
                .And(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CarryingTheNeededItemsCompletesTheQuest()
        {
            await new Spec("Carrying the items a needed quest asks for completes it and takes nothing")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Needed, 3, QuestItem)
                .And(TheCharacterCarries_Of_, 3, QuestItem)
                .WhenAsync(TheCharacterReceives_, QuestItem)
                .Then(TheQuestShouldBeCompleted)
                .And(TheCharacterShouldStillCarry_Of_, 3, QuestItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CarryingTooFewNeededItemsDoesNotComplete()
        {
            await new Spec("A needed quest stays open while fewer items than asked are carried")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Needed, 3, QuestItem)
                .And(TheCharacterCarries_Of_, 2, QuestItem)
                .WhenAsync(TheCharacterReceives_, QuestItem)
                .Then(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NeededQuestIgnoresOtherItems()
        {
            await new Spec("Receiving an item a needed quest does not ask for changes nothing")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Needed, 3, QuestItem)
                .And(TheCharacterCarries_Of_, 3, OtherItem)
                .WhenAsync(TheCharacterReceives_, OtherItem)
                .Then(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BringingTheItemsToTheNpcHandsThemIn()
        {
            await new Spec("Talking to the NPC of a brings quest with the items takes them and completes it")
                .Given(TheCharacterHasA_QuestToBring_Of_To_, QuestType.Brings, 3, QuestItem, Npc)
                .And(TheCharacterCarries_Of_, 4, QuestItem)
                .WhenAsync(TheCharacterTalksTo_, Npc)
                .Then(TheObjectiveShouldRead_, 3)
                .And(TheQuestShouldBeCompleted)
                .And(TheCharacterShouldStillCarry_Of_, 1, QuestItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CarryingTheItemsWithoutTalkingToTheNpcDoesNotCount()
        {
            await new Spec("Picking up a brings quest's items does not complete it before the hand-in")
                .Given(TheCharacterHasA_QuestToBring_Of_To_, QuestType.Brings, 3, QuestItem, Npc)
                .And(TheCharacterCarries_Of_, 3, QuestItem)
                .WhenAsync(TheCharacterReceives_, QuestItem)
                .Then(TheObjectiveShouldRead_, 0)
                .And(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BringingTooFewItemsTakesNothing()
        {
            await new Spec("Talking to the NPC with fewer items than asked keeps the items and the quest open")
                .Given(TheCharacterHasA_QuestToBring_Of_To_, QuestType.Brings, 3, QuestItem, Npc)
                .And(TheCharacterCarries_Of_, 2, QuestItem)
                .WhenAsync(TheCharacterTalksTo_, Npc)
                .Then(TheQuestShouldNotBeCompleted)
                .And(TheCharacterShouldStillCarry_Of_, 2, QuestItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BringingTheItemsToAnotherNpcTakesNothing()
        {
            await new Spec("Talking to an NPC the quest does not name keeps the items and the quest open")
                .Given(TheCharacterHasA_QuestToBring_Of_To_, QuestType.Brings, 3, QuestItem, Npc)
                .And(TheCharacterCarries_Of_, 3, QuestItem)
                .WhenAsync(TheCharacterTalksTo_, OtherNpc)
                .Then(TheQuestShouldNotBeCompleted)
                .And(TheCharacterShouldStillCarry_Of_, 3, QuestItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BringingAnotherItemTakesNothing()
        {
            await new Spec("Talking to the NPC with only other items keeps them and the quest open")
                .Given(TheCharacterHasA_QuestToBring_Of_To_, QuestType.Brings, 3, QuestItem, Npc)
                .And(TheCharacterCarries_Of_, 3, OtherItem)
                .WhenAsync(TheCharacterTalksTo_, Npc)
                .Then(TheQuestShouldNotBeCompleted)
                .And(TheCharacterShouldStillCarry_Of_, 3, OtherItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandingInRequiredItemsCompletesTheQuest()
        {
            await new Spec("Talking to the NPC of a required quest with the items takes them and completes it")
                .Given(TheCharacterHasA_QuestToBring_Of_To_, QuestType.Required, 2, QuestItem, Npc)
                .And(TheCharacterCarries_Of_, 2, QuestItem)
                .WhenAsync(TheCharacterTalksTo_, Npc)
                .Then(TheQuestShouldBeCompleted)
                .And(TheCharacterShouldStillCarry_Of_, 0, QuestItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequiredQuestIgnoresOtherItems()
        {
            await new Spec("Talking to the NPC of a required quest with other items takes nothing")
                .Given(TheCharacterHasA_QuestToBring_Of_To_, QuestType.Required, 2, QuestItem, Npc)
                .And(TheCharacterCarries_Of_, 2, OtherItem)
                .WhenAsync(TheCharacterTalksTo_, Npc)
                .Then(TheQuestShouldNotBeCompleted)
                .And(TheCharacterShouldStillCarry_Of_, 2, OtherItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProducingAdvancesTheQuestByTheRecipeAmount()
        {
            await new Spec("Producing the item of a product quest adds the produced amount to its counter")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Product, 5, QuestItem)
                .WhenAsync(TheCharacterProduces_Of_, 2, QuestItem)
                .Then(TheObjectiveShouldRead_, 2)
                .And(TheProgressShouldBeSentToTheClient)
                .And(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProducingTheLastItemsCompletesTheQuest()
        {
            await new Spec("Producing enough of the item caps the counter and completes the product quest")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Product, 3, QuestItem)
                .AndAsync(TheCharacterProduces_Of_, 2, QuestItem)
                .WhenAsync(TheCharacterProduces_Of_, 2, QuestItem)
                .Then(TheObjectiveShouldRead_, 3)
                .And(TheQuestShouldBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpTheProductDoesNotCount()
        {
            await new Spec("Receiving the item of a product quest any other way leaves its counter alone")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Product, 3, QuestItem)
                .And(TheCharacterCarries_Of_, 3, QuestItem)
                .WhenAsync(TheCharacterReceives_, QuestItem)
                .Then(TheObjectiveShouldRead_, 0)
                .And(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProducingAnotherItemIsIgnored()
        {
            await new Spec("Producing an item a product quest does not ask for changes nothing")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Product, 3, QuestItem)
                .WhenAsync(TheCharacterProduces_Of_, 3, OtherItem)
                .Then(TheObjectiveShouldRead_, 0)
                .And(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingTheItemCompletesTheQuest()
        {
            await new Spec("Using the item of a use quest completes it")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Use, 0, QuestItem)
                .And(TheCharacterCarries_Of_, 1, QuestItem)
                .WhenAsync(TheCharacterUses_, QuestItem)
                .Then(TheQuestShouldBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingAnotherItemIsIgnored()
        {
            await new Spec("Using an item a use quest does not name leaves it open")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Use, 0, QuestItem)
                .And(TheCharacterCarries_Of_, 1, OtherItem)
                .WhenAsync(TheCharacterUses_, OtherItem)
                .Then(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingTheItemCompletesTheQuest()
        {
            await new Spec("Wearing the item of a wear quest completes it")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Wear, 0, (short)1)
                .WhenAsync(TheCharacterWears_, (short)1)
                .Then(TheQuestShouldBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingAnotherItemIsIgnored()
        {
            await new Spec("Wearing an item a wear quest does not name leaves it open")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.Wear, 0, (short)1)
                .WhenAsync(TheCharacterWears_, (short)924)
                .Then(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherQuestTypesAreLeftAlone()
        {
            await new Spec("Item events do not touch quests of other types")
                .Given(TheCharacterHasA_QuestFor_Of_, QuestType.GoTo, 0, QuestItem)
                .And(TheCharacterCarries_Of_, 1, QuestItem)
                .WhenAsync(TheCharacterUses_, QuestItem)
                .Then(TheQuestShouldNotBeCompleted)
                .ExecuteAsync();
        }

        private void TheCharacterHasA_QuestFor_Of_(QuestType type, int amount, short vNum) =>
            GiveQuest(type, new QuestObjectiveDto
            {
                QuestObjectiveId = Guid.NewGuid(), QuestId = 1, FirstData = vNum, SecondData = amount == 0 ? null : amount
            });

        private void TheCharacterHasA_QuestToBring_Of_To_(QuestType type, int amount, short vNum, short npc) =>
            GiveQuest(type, new QuestObjectiveDto
            {
                QuestObjectiveId = Guid.NewGuid(), QuestId = 1, FirstData = npc, SecondData = amount, ThirdData = vNum
            });

        private void GiveQuest(QuestType type, QuestObjectiveDto objective)
        {
            _objective = objective;
            _quest = new CharacterQuest
            {
                Id = Guid.NewGuid(),
                CharacterId = _session.Character.CharacterId,
                QuestId = 1,
                Quest = new GameObject.Services.QuestService.Quest
                {
                    QuestId = 1,
                    QuestType = type,
                    QuestObjectives = new List<QuestObjectiveDto> { objective }
                }
            };
            _session.Character.Quests.TryAdd(_quest.Id, _quest);
        }

        private void TheCharacterCarries_Of_(int amount, short vNum) =>
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_itemProvider.Create(vNum, (short)amount), _session.Character.CharacterId));

        private Task TheCharacterReceives_(short vNum) =>
            _handler.Handle(new ItemsReceivedEvent(_session.Character, vNum, 1));

        private Task TheCharacterProduces_Of_(int amount, short vNum) =>
            _handler.Handle(new ItemProducedEvent(_session.Character, vNum, (short)amount));

        private Task TheCharacterTalksTo_(short npcVNum)
        {
            var npc = new Mock<INonPlayableEntity>();
            npc.SetupGet(n => n.NpcMonster).Returns(new NpcMonsterDto { NpcMonsterVNum = npcVNum });
            return _handler.Handle(new NpcTalkedEvent(_session.Character, npc.Object));
        }

        private Task TheCharacterWears_(short vNum) =>
            _handler.Handle(new ItemWornEvent(_session.Character, vNum));

        private Task TheCharacterUses_(short vNum)
        {
            var item = _session.Character.InventoryService.Values.First(s => s.ItemInstance.ItemVNum == vNum);
            return _handler.Handle(new ItemUsedEvent(_session, item, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = (PocketType)item.Type,
                Slot = item.Slot
            }));
        }

        private void TheObjectiveShouldRead_(int expected) =>
            Assert.AreEqual(expected, _quest.ObjectiveProgress.TryGetValue(_objective.QuestObjectiveId, out var c) ? c : 0);

        private void TheProgressShouldBeSentToTheClient() =>
            Assert.AreEqual(_quest.QuestId, _session.LastPackets.OfType<QstiPacket>().Last().QuestSubPacket.QuestId);

        private void TheQuestShouldBeCompleted() =>
            _questService.Verify(s => s.CompleteQuestAsync(It.IsAny<ICharacterEntity>(), _quest), Times.Once);

        private void TheQuestShouldNotBeCompleted() =>
            _questService.Verify(s => s.CompleteQuestAsync(It.IsAny<ICharacterEntity>(), It.IsAny<CharacterQuest>()), Times.Never);

        private void TheCharacterShouldStillCarry_Of_(int expected, short vNum) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(vNum));
    }
}

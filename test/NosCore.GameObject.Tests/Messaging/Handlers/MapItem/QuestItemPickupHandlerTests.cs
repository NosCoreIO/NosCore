//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using Wolverine;

namespace NosCore.GameObject.Tests.Messaging.Handlers.MapItem
{
    [TestClass]
    public class QuestItemPickupHandlerTests
    {
        private const short Feather = 1099;
        private TestWorld World;
        private ClientSession _session;
        private Mock<IMessageBus> _messageBus;
        private long _visualId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _messageBus = World.Freeze<Mock<IMessageBus>>();
        }

        [TestMethod]
        public async Task PickingUpARaidQuestItemCountsItWithoutFillingTheBag()
        {
            await new Spec("A raid collect quest's map item leaves the map and counts as received, but never reaches the bag")
                .Given(A_MapItemLiesNextToTheCharacter, ItemEffectType.RaidCollectQuestItem)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheItemShouldBeGoneFromTheMap)
                .And(TheReceivedItemShouldBePublished_Times, 1)
                .And(TheBagShouldNotHoldIt)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherMapItemsAreLeftToTheirOwnHandlers()
        {
            await new Spec("A map item with another effect is not picked up as a quest item")
                .Given(A_MapItemLiesNextToTheCharacter, ItemEffectType.NoEffect)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheReceivedItemShouldBePublished_Times, 0)
                .ExecuteAsync();
        }

        private void A_MapItemLiesNextToTheCharacter(ItemEffectType effect)
        {
            World.ItemList.Add(new ItemDto
            {
                VNum = Feather, Type = NoscorePocketType.Etc, ItemType = ItemType.Map, Effect = effect,
                Name = new I18NString { [RegionType.EN] = "Feather" }
            });
            _visualId = World.MapItemProvider.NotNull().Create(_session.Character.MapInstance,
                World.GenerateItemProvider().Create(Feather, 1), _session.Character.PositionX, _session.Character.PositionY,
                _session.Character.CharacterId).VisualId;
        }

        private Task TheCharacterPicksItUp()
        {
            var mapItem = _session.Character.MapInstance.TryGetMapItem(_visualId).NotNull();
            return World.Create<QuestItemPickupHandler>().Handle(new MapItemPickedUpEvent(_session, _visualId, Feather, 1,
                mapItem.ItemInstance, new GetPacket { PickerType = VisualType.Player, VisualId = _visualId }));
        }

        private void TheItemShouldBeGoneFromTheMap() =>
            Assert.IsNull(_session.Character.MapInstance.TryGetMapItem(_visualId));

        private void TheReceivedItemShouldBePublished_Times(int times) =>
            _messageBus.Verify(b => b.PublishAsync(It.Is<ItemsReceivedEvent>(e => e.VNum == Feather && e.Amount == 1),
                It.IsAny<DeliveryOptions?>()), Times.Exactly(times));

        private void TheBagShouldNotHoldIt() =>
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(Feather));
    }
}

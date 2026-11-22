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
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.MapItem
{
    [TestClass]
    public class QuestItemPickupHandlerTests
    {
        private const short Feather = 1099;
        private TestWorld World;
        private ClientSession _session;
        private Mock<IGameEventDispatcher> _gameEvents;
        private long _visualId;
        private ClientSession _here;
        private ClientSession _elsewhere;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _gameEvents = World.Freeze<Mock<IGameEventDispatcher>>();
        }

        [TestMethod]
        public async Task PickingUpARaidQuestItemCountsItWithoutFillingTheBag()
        {
            await new Spec("A raid collect quest's map item leaves the map and counts as received, but never reaches the bag")
                .Given(A_MapItemLiesNextToTheCharacter, ItemEffectType.RaidCollectQuestItem)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheItemShouldBeGoneFromTheMap)
                .And(TheReceivedItemShouldBeRaised_Times, 1)
                .And(TheBagShouldNotHoldIt)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARaidQuestItemCountsForThePartyMembersOnTheMap()
        {
            await new Spec("A raid collect quest item picked up is passed on to each party member on the map, not to the picker or a member elsewhere")
                .Given(A_MapItemLiesNextToTheCharacter, ItemEffectType.RaidCollectQuestItem)
                .AndAsync(TheCharacterIsInAPartyWithOneMemberHereAndOneElsewhere)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(ThePartyPickupShouldReachOnlyTheMemberOnTheMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherMapItemsAreLeftToTheirOwnHandlers()
        {
            await new Spec("A map item with another effect is not picked up as a quest item")
                .Given(A_MapItemLiesNextToTheCharacter, ItemEffectType.NoEffect)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheReceivedItemShouldBeRaised_Times, 0)
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

        private async Task TheCharacterIsInAPartyWithOneMemberHereAndOneElsewhere()
        {
            _here = await World.CreateSessionAsync();
            _elsewhere = await World.CreateSessionAsync();
            await World.MapChangeService.ChangeMapInstanceAsync(_elsewhere, World.GetBaseMap(TestWorld.ShopMapId).MapInstanceId, 1, 1);
            var party = new Group(GroupType.Group, World.SessionGroupFactory);
            foreach (var member in new[] { _session, _here, _elsewhere })
            {
                member.Character.JoinGroup(party);
            }
        }

        private void ThePartyPickupShouldReachOnlyTheMemberOnTheMap()
        {
            var here = _here.Character.VisualId;
            _gameEvents.Verify(b => b.RaiseAsync(It.Is<PartyItemPickedUpEvent>(e => e.VNum == Feather
                && e.Character.VisualId == here)), Times.Once);
            _gameEvents.Verify(b => b.RaiseAsync(It.IsAny<PartyItemPickedUpEvent>()), Times.Once);
        }

        private Task TheCharacterPicksItUp()
        {
            var mapItem = _session.Character.MapInstance.TryGetMapItem(_visualId).NotNull();
            return World.Create<QuestItemPickupHandler>().Handle(new MapItemPickedUpEvent(_session, _visualId, Feather, 1,
                mapItem.ItemInstance, new GetPacket { PickerType = VisualType.Player, VisualId = _visualId }));
        }

        private void TheItemShouldBeGoneFromTheMap() =>
            Assert.IsNull(_session.Character.MapInstance.TryGetMapItem(_visualId));

        private void TheReceivedItemShouldBeRaised_Times(int times) =>
            _gameEvents.Verify(b => b.RaiseAsync(It.Is<ItemsReceivedEvent>(e => e.VNum == Feather && e.Amount == 1)), Times.Exactly(times));

        private void TheBagShouldNotHoldIt() =>
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(Feather));
    }
}

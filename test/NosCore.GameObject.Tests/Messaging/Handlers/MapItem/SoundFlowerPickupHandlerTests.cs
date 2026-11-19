//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.MapItem
{
    [TestClass]
    public class SoundFlowerPickupHandlerTests
    {
        private const short WildSoundFlower = 1086;

        private TestWorld World;
        private ClientSession _session;
        private Mock<ISoundFlowerService> _soundFlowers;
        private readonly Mock<ISessionGroup> _mapSessions = new();
        private long _visualId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance.Sessions = _mapSessions.Object;
            _soundFlowers = World.Freeze<Mock<ISoundFlowerService>>();
        }

        [TestMethod]
        public async Task PickingUpASoundFlowerStartsItsQuestWithoutFillingTheBag()
        {
            await new Spec("A Sound Flower picked up starts its quest and leaves the map, but never reaches the bag")
                .Given(A_LiesNextToTheCharacter, ItemEffectType.SoundFlower)
                .And(TheQuestCanBeTaken_, true)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(AFlowerQuestIsAskedFor_Times, 1)
                .And(TheFlowerIsGoneFromTheMap)
                .And(TheMapIsShownThePickUp_Times, 1)
                .And(TheBagDoesNotHoldIt)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFlowerWhoseQuestIsRefusedStaysOnTheGround()
        {
            await new Spec("A Sound Flower whose quest cannot be taken stays on the ground and out of the bag")
                .Given(A_LiesNextToTheCharacter, ItemEffectType.SoundFlower)
                .And(TheQuestCanBeTaken_, false)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheFlowerStaysOnTheMap)
                .And(TheMapIsShownThePickUp_Times, 0)
                .And(TheBagDoesNotHoldIt)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherMapItemsAreLeftToTheirOwnHandlers()
        {
            await new Spec("A map item with another effect starts no flower quest")
                .Given(A_LiesNextToTheCharacter, ItemEffectType.NoEffect)
                .And(TheQuestCanBeTaken_, true)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(AFlowerQuestIsAskedFor_Times, 0)
                .And(TheFlowerStaysOnTheMap)
                .ExecuteAsync();
        }

        private void A_LiesNextToTheCharacter(ItemEffectType effect)
        {
            World.ItemList.Add(new ItemDto
            {
                VNum = WildSoundFlower, Type = NoscorePocketType.Main, ItemType = ItemType.Map, Effect = effect,
                Name = new I18NString { [RegionType.EN] = "Wild Sound Flower" }
            });
            _visualId = World.MapItemProvider.NotNull().Create(_session.Character.MapInstance,
                World.GenerateItemProvider().Create(WildSoundFlower, 1), _session.Character.PositionX, _session.Character.PositionY,
                _session.Character.CharacterId).VisualId;
        }

        private void TheQuestCanBeTaken_(bool taken) =>
            _soundFlowers.Setup(s => s.StartFlowerQuestAsync(It.IsAny<PlayerComponentBundle>())).ReturnsAsync(taken);

        private Task TheCharacterPicksItUp()
        {
            var mapItem = _session.Character.MapInstance.TryGetMapItem(_visualId).NotNull();
            return World.Create<SoundFlowerPickupHandler>().Handle(new MapItemPickedUpEvent(_session, _visualId, WildSoundFlower, 1,
                mapItem.ItemInstance, new GetPacket
                {
                    PickerType = VisualType.Player, PickerId = _session.Character.CharacterId, VisualId = _visualId
                }));
        }

        private void AFlowerQuestIsAskedFor_Times(int times) =>
            _soundFlowers.Verify(s => s.StartFlowerQuestAsync(It.IsAny<PlayerComponentBundle>()), Times.Exactly(times));

        private void TheFlowerIsGoneFromTheMap() =>
            Assert.IsNull(_session.Character.MapInstance.TryGetMapItem(_visualId));

        private void TheFlowerStaysOnTheMap() =>
            Assert.IsNotNull(_session.Character.MapInstance.TryGetMapItem(_visualId));

        private void TheMapIsShownThePickUp_Times(int times) =>
            Assert.AreEqual(times, _mapSessions.Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>())
                .SelectMany(packets => packets).OfType<ServerGetPacket>()
                .Count(p => p.VisualType == VisualType.Player && p.VisualId == _session.Character.CharacterId && p.ItemId == _visualId));

        private void TheBagDoesNotHoldIt() =>
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(WildSoundFlower));
    }
}

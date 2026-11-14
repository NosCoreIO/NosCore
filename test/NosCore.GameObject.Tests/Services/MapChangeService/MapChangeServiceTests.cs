//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Tests.Shared;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.GroupService;
using NosCore.Shared.Enumerations;
using System.Linq;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Tests.Services.MapChangeService
{
    [TestClass]
    public class MapChangeServiceTests
    {
        private TestWorld World;
        private static readonly ILogger<NosCore.GameObject.Services.MapChangeService.MapChangeService> Logger = new Mock<ILogger<NosCore.GameObject.Services.MapChangeService.MapChangeService>>().Object;
        private IMapChangeService Service;
        private ClientSession Session;
        private Mock<IMinilandService> MinilandService;
        private readonly Mock<GameObject.Services.ScriptedInstanceService.IScriptedInstanceService> ScriptedInstances = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(0);

            var minilandService = new Mock<IMinilandService>();
            MinilandService = minilandService;
            minilandService.Setup(s => s.GetMinilandPortals(It.IsAny<long>()))
                .Returns(new System.Collections.Generic.List<Map.Portal>());

            Service = new GameObject.Services.MapChangeService.MapChangeService(
                new Mock<IExperienceService>().Object,
                new Mock<IJobExperienceService>().Object,
                new Mock<IHeroExperienceService>().Object, new NosCore.Algorithm.SkillPointService.SkillPointService(),
                World.MapInstanceAccessorService,
                World.Clock,
                World.LogLanguageLocalizer,
                minilandService.Object,
                Logger,
                World.LogLanguageLocalizer,
                World.GameLanguageLocalizer,
                World.SessionRegistry,
                new Mock<Wolverine.IMessageBus>().Object,
                ScriptedInstances.Object,
                new Mock<NosCore.GameObject.Services.BattleService.IBattleStatsProvider>().Object);
        }

        [TestMethod]
        public async Task ChangingToNonExistentMapShouldNotChangeMapInstance()
        {
            await new Spec("Changing to non-existent map should not change map instance")
                .Given(CharacterIsOnMap)
                .WhenAsync(ChangingToNonExistentMap)
                .Then(MapInstanceShouldNotChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingMapWithNullCoordinatesShouldNotCrash()
        {
            await new Spec("Changing map with null coordinates should not crash")
                .WhenAsync(ChangingMapWithNullCoordinates)
                .Then(NoExceptionShouldBeThrown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ServiceCanBeConstructed()
        {
            await new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MapAccessorReturnsValidMaps()
        {
            await new Spec("Map accessor returns valid maps")
                .Then(Map0ShouldExist)
                .And(Map1ShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingToBaseMapInstanceUpdatesMapId()
        {
            await new Spec("Changing to a BaseMap instance overwrites character.MapId with the new map id")
                .Given(CharacterIsOnMap)
                .And(CharacterMapIdIsPinnedTo_, (short)42)
                .WhenAsync(ChangingToBaseMap1)
                .Then(CharacterMapIdShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingToNonBaseInstanceKeepsMapIdUnchanged()
        {
            await new Spec("Changing to a non-base instance leaves character.MapId at its previous base-map value")
                .Given(CharacterIsOnMap)
                .And(CharacterMapIdIsPinnedTo_, (short)42)
                .WhenAsync(ChangingToMinilandInstance)
                .Then(CharacterMapIdShouldBe_, (short)42)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TimeSpaceStonesAreOnlyPlacedOnTheOpenWorld()
        {
            await ChangingToBaseMap1();
            ScriptedInstances.Verify(s => s.GenerateWp(1), Times.Once);

            await ChangingToMinilandInstance();
            ScriptedInstances.Verify(s => s.GenerateWp(It.IsAny<short>()), Times.Once);
        }

        private sealed class EmptyScript : NosCore.Instances.Abstractions.InstanceScript;

        private GameObject.Services.ScriptedInstanceService.ScriptedInstanceRun Run() =>
            new(new GameObject.Services.ScriptedInstanceService.TimeSpaceRunHost(
                    new GameObject.Services.ScriptedInstanceService.ScriptedInstance(),
                    new System.Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object)),
                new EmptyScript(),
                new System.Collections.Generic.Dictionary<int, MapInstance>(),
                new System.Collections.Generic.Dictionary<short, NosCore.Data.StaticEntities.NpcMonsterDto>(),
                new GameObject.Services.BattleService.RandomProvider(), World.Clock, World.SessionRegistry,
                World.Create<GameObject.Services.BattleService.AggroService>(), NullLogger.Instance, World.LogLanguageLocalizer, System.TimeSpan.FromSeconds(30));

        [TestMethod]
        public async Task WalkingIntoTheNextRoomKeepsTheTimeSpaceRunning()
        {
            var run = Run();
            ScriptedInstances.Setup(s => s.GetRun(It.IsAny<System.Guid>())).Returns(run);

            await ChangingToBaseMap1();

            ScriptedInstances.Verify(s => s.DisposeIfEmptyAsync(It.IsAny<GameObject.Services.ScriptedInstanceService.ScriptedInstanceRun>()), Times.Never);
        }

        [TestMethod]
        public async Task LeavingATimeSpaceDisposesItsRunWhenEmpty()
        {
            var run = Run();
            var current = Session.Character.MapInstance.MapInstanceId;
            ScriptedInstances.Setup(s => s.GetRun(current)).Returns(run);

            await ChangingToBaseMap1();

            ScriptedInstances.Verify(s => s.DisposeIfEmptyAsync(run), Times.Once);
        }

        [TestMethod]
        public async Task WhatIsBeingDigestedSurvivesAMapChange()
        {
            await new Spec("The digestion state moves with the character to the new map")
                .Given(CharacterIsOnMap)
                .And(TheCharacterIsDigesting)
                .WhenAsync(ChangingToBaseMap1)
                .Then(TheSameDigestionStateShouldStillBeAttached)
                .ExecuteAsync();
        }

        private DigestionState _digestion;

        private void TheCharacterIsDigesting()
        {
            _digestion = Session.Character.Digestion;
            Assert.IsNotNull(_digestion);
            _digestion.Snack.Add(50, 50);
        }

        private void TheSameDigestionStateShouldStillBeAttached()
        {
            Assert.AreSame(_digestion, Session.Character.Digestion);
            Assert.IsFalse(Session.Character.Digestion.Snack.IsEmpty);
        }

        private void CharacterMapIdIsPinnedTo_(short mapId)
        {
            Session.Character.MapId = mapId;
        }

        [TestMethod]
        public async Task AMapChangeListsThePetInThePartyPanel()
        {
            var pet = TestMates.Pet(Session.Character.CharacterId);
            Session.Character.Mates[pet.MateTransportId] = pet;

            await ChangingToBaseMap1();

            var pinit = Session.LastPackets.OfType<PinitPacket>().Last();
            Assert.IsTrue(pinit.PinitSubPackets.NotNull().Any(s => s.NotNull().VisualId == pet.MateTransportId));
            Assert.IsNotNull(pet.Entity, "the pet must be on the new map before the panel names it");
        }

        [TestMethod]
        public async Task NosMatesAtHomeStandInTheOwnersMiniland()
        {
            var ownerId = Session.Character.CharacterId;
            MinilandService.Setup(s => s.GetMinilandFromMapInstanceId(World.MinilandId))
                .Returns(new Miniland { OwnerId = ownerId });
            var home = TestMates.Pet(ownerId, false);
            home.MapX = 4;
            home.MapY = 1;
            Session.Character.Mates[home.MateTransportId] = home;

            await ChangingToMinilandInstance();

            Assert.IsNotNull(home.Entity);
            Assert.AreEqual(((short)4, (short)1), (home.Entity.NotNull().PositionX, home.Entity.NotNull().PositionY));
            Assert.IsFalse(Session.LastPackets.OfType<PinitPacket>().Last().PinitSubPackets.NotNull()
                .Any(s => s.NotNull().VisualId == home.MateTransportId), "a NosMate at home is not in the party panel");
        }

        [TestMethod]
        public async Task NosMatesAtHomeStayOffOtherMaps()
        {
            var home = TestMates.Pet(Session.Character.CharacterId, false);
            Session.Character.Mates[home.MateTransportId] = home;

            await ChangingToBaseMap1();

            Assert.IsNull(home.Entity);
        }

        [TestMethod]
        public async Task InAGroupThePanelArrivesBeforeTheMembersBars()
        {
            var other = await World.CreateSessionAsync();
            var group = new Group(GroupType.Group, World.SessionGroupFactory);
            Session.Character.JoinGroup(group);
            other.Character.JoinGroup(group);
            Session.LastPackets.Clear();

            await ChangingToBaseMap1();

            var sent = Session.LastPackets.ToList();
            var panel = sent.FindLastIndex(p => p is PinitPacket);
            var firstBar = sent.FindIndex(p => p is PstPacket s && s.Type == VisualType.Player);
            Assert.IsTrue(panel >= 0 && firstBar > panel, "a pinit rebuilds the panel, so bars sent before it are lost");
        }

        private async Task ChangingToBaseMap1()
        {
            var map1 = World.GetBaseMap(1);
            await Service.ChangeMapInstanceAsync(Session, map1.MapInstanceId, 10, 10);
        }

        private async Task ChangingToMinilandInstance()
        {
            await Service.ChangeMapInstanceAsync(Session, World.MinilandId, 5, 8);
        }

        private void CharacterMapIdShouldBe_(short expected)
        {
            Assert.AreEqual(expected, Session.Character.MapId);
        }

        private short OriginalMapId;
        private bool OperationCompleted;

        private void CharacterIsOnMap()
        {
            OriginalMapId = Session.Character.MapInstance.Map.MapId;
        }

        private async Task ChangingToNonExistentMap()
        {
            await Service.ChangeMapAsync(Session, 9999, 10, 20);
            OperationCompleted = true;
        }

        private async Task ChangingMapWithNullCoordinates()
        {
            await Service.ChangeMapAsync(Session, null, null, null);
            OperationCompleted = true;
        }

        private void MapInstanceShouldNotChange()
        {
            Assert.AreEqual(OriginalMapId, Session.Character.MapInstance.Map.MapId);
        }

        private void NoExceptionShouldBeThrown()
        {
            Assert.IsTrue(OperationCompleted);
        }

        private void ServiceShouldNotBeNull()
        {
            Assert.IsNotNull(Service);
        }

        private void Map0ShouldExist()
        {
            var map0 = World.GetBaseMap(0);
            Assert.IsNotNull(map0);
        }

        private void Map1ShouldExist()
        {
            var map1 = World.GetBaseMap(1);
            Assert.IsNotNull(map1);
        }
    }
}

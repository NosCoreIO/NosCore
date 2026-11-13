//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.CharacterService
{
    [TestClass]
    public class CharacterInitializationServiceTests
    {
        private TestWorld World;
        private static readonly ILogger<CharacterInitializationServiceTests> Logger = new Mock<ILogger<CharacterInitializationServiceTests>>().Object;
        private ICharacterInitializationService Service;
        private IMinilandService MinilandService;
        private ClientSession Session;
        private Mock<IFriendHub> FriendHttpClient;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();

            FriendHttpClient = new Mock<IFriendHub>();
            FriendHttpClient.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());

            await World.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto
            {
                OwnerId = Session.Character.CharacterId
            });

            await World.SeedMinilandEntrancesAsync();

            MinilandService = new GameObject.Services.MinilandService.MinilandService(
                World.MapInstanceAccessorService,
                FriendHttpClient.Object,
                new List<MapDto>
                {
                    new NosCore.GameObject.Map.Map
                    {
                        MapId = 20001,
                        NameI18NKey = "miniland",
                        Data = new byte[] { }
                    }
                },
                World.MinilandDao,
                World.MinilandObjectDao,
                World.PortalDao,
                World.Create<MinilandRegistry>());

            Service = new CharacterInitializationService(MinilandService, World.MapInstanceGeneratorService);
        }

        [TestMethod]
        public async Task InitializingCharacterShouldInitializeMiniland()
        {
            await new Spec("Initializing character should initialize miniland")
                .WhenAsync(InitializingCharacter)
                .Then(MinilandShouldBeInitialized)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InitializingCharacterWithNullServicesShouldDoNothing()
        {
            await new Spec("Initializing character with null services should do nothing")
                .Given(ServiceWithNullDependencies)
                .WhenAsync(InitializingCharacterWithNullServices)
                .Then(NoExceptionShouldBeThrown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InitializingCharacterWithOnlyMinilandServiceShouldDoNothing()
        {
            await new Spec("Initializing character with only miniland service should do nothing")
                .Given(ServiceWithOnlyMinilandService)
                .WhenAsync(InitializingCharacterWithPartialServices)
                .Then(NoExceptionShouldBeThrown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InitializingCharacterWithOnlyMapInstanceServiceShouldDoNothing()
        {
            await new Spec("Initializing character with only map instance service should do nothing")
                .Given(ServiceWithOnlyMapInstanceService)
                .WhenAsync(InitializingCharacterWithPartialServices)
                .Then(NoExceptionShouldBeThrown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InitializingMultipleCharactersShouldWork()
        {
            await new Spec("Initializing multiple characters should work")
                .WhenAsync(InitializingMultipleCharacters)
                .Then(BothMinilandsShouldBeInitialized)
                .ExecuteAsync();
        }

        private ICharacterInitializationService NullService;
        private ICharacterInitializationService PartialService;
        private ClientSession SecondSession;
        private bool InitializationCompleted;
        private bool SecondInitializationCompleted;

        private async Task InitializingCharacter()
        {
            await Service.InitializeAsync(Session.Character);
            InitializationCompleted = true;
        }

        private void ServiceWithNullDependencies()
        {
            NullService = new CharacterInitializationService();
        }

        private async Task InitializingCharacterWithNullServices()
        {
            await NullService.InitializeAsync(Session.Character);
            InitializationCompleted = true;
        }

        private void ServiceWithOnlyMinilandService()
        {
            PartialService = new CharacterInitializationService(MinilandService);
        }

        private void ServiceWithOnlyMapInstanceService()
        {
            PartialService = new CharacterInitializationService(mapInstanceGeneratorService: World.MapInstanceGeneratorService);
        }

        private async Task InitializingCharacterWithPartialServices()
        {
            await PartialService.InitializeAsync(Session.Character);
            InitializationCompleted = true;
        }

        private async Task InitializingMultipleCharacters()
        {
            SecondSession = await World.CreateSessionAsync();
            await World.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto
            {
                OwnerId = SecondSession.Character.CharacterId
            });

            await Service.InitializeAsync(Session.Character);
            InitializationCompleted = true;

            await Service.InitializeAsync(SecondSession.Character);
            SecondInitializationCompleted = true;
        }

        private void MinilandShouldBeInitialized()
        {
            Assert.IsTrue(InitializationCompleted);
            var miniland = MinilandService.GetMiniland(Session.Character.CharacterId);
            Assert.IsNotNull(miniland);
        }

        private void NoExceptionShouldBeThrown()
        {
            Assert.IsTrue(InitializationCompleted);
        }

        private void BothMinilandsShouldBeInitialized()
        {
            Assert.IsTrue(InitializationCompleted);
            Assert.IsTrue(SecondInitializationCompleted);

            var miniland1 = MinilandService.GetMiniland(Session.Character.CharacterId);
            var miniland2 = MinilandService.GetMiniland(SecondSession.Character.CharacterId);

            Assert.IsNotNull(miniland1);
            Assert.IsNotNull(miniland2);
            Assert.AreNotEqual(miniland1.MapInstanceId, miniland2.MapInstanceId);
        }
    }
}

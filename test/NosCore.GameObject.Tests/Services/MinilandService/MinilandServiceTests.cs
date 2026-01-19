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
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MinilandService
{
    [TestClass]
    public class MinilandServiceTests
    {
        private IMinilandService Service = null!;
        private Mock<IFriendHub> FriendHttpClient = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            FriendHttpClient = new Mock<IFriendHub>();
            FriendHttpClient.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());

            await TestHelpers.Instance.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto
            {
                OwnerId = Session.Character.CharacterId
            });

            Service = new GameObject.Services.MinilandService.MinilandService(
                TestHelpers.Instance.MapInstanceAccessorService,
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
                TestHelpers.Instance.MinilandDao,
                TestHelpers.Instance.MinilandObjectDao,
                new MinilandRegistry());
        }

        [TestMethod]
        public async Task InitializingMinilandShouldSucceed()
        {
            await new Spec("Initializing miniland should succeed")
                .WhenAsync(InitializingMiniland)
                .Then(MinilandShouldBeInitialized)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GettingMinilandShouldReturnMiniland()
        {
            await new Spec("Getting miniland should return miniland")
                .GivenAsync(MinilandIsInitialized)
                .When(GettingMiniland)
                .Then(MinilandShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GettingNonExistentMinilandShouldThrow()
        {
            await new Spec("Getting non-existent miniland should throw")
                .When(GettingNonExistentMiniland).Catch(out var exception)
                .Then(ShouldThrowArgumentException_, exception)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingMinilandStateToOpenShouldSucceed()
        {
            await new Spec("Setting miniland state to open should succeed")
                .GivenAsync(MinilandIsInitialized)
                .WhenAsync(SettingStateToOpen)
                .Then(StateShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingMinilandStateToPrivateShouldSucceed()
        {
            await new Spec("Setting miniland state to private should succeed")
                .GivenAsync(MinilandIsInitialized)
                .WhenAsync(SettingStateToPrivate)
                .Then(StateShouldBePrivate)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingMinilandStateToLockShouldSucceed()
        {
            await new Spec("Setting miniland state to lock should succeed")
                .GivenAsync(MinilandIsInitialized)
                .WhenAsync(SettingStateToLock)
                .Then(StateShouldBeLock)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingMinilandShouldSucceed()
        {
            await new Spec("Deleting miniland should succeed")
                .GivenAsync(MinilandIsInitialized)
                .WhenAsync(DeletingMiniland)
                .Then(DeleteShouldSucceed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingNonExistentMinilandShouldReturnNull()
        {
            await new Spec("Deleting non-existent miniland should return null")
                .WhenAsync(DeletingNonExistentMiniland)
                .Then(DeleteShouldReturnNull)
                .ExecuteAsync();
        }

        private Miniland? InitializedMiniland;
        private Miniland? RetrievedMiniland;
        private Guid? DeleteResult;

        private async Task InitializingMiniland()
        {
            InitializedMiniland = await Service.InitializeAsync(Session.Character, TestHelpers.Instance.MapInstanceGeneratorService);
        }

        private async Task MinilandIsInitialized()
        {
            InitializedMiniland = await Service.InitializeAsync(Session.Character, TestHelpers.Instance.MapInstanceGeneratorService);
        }

        private void GettingMiniland()
        {
            RetrievedMiniland = Service.GetMiniland(Session.Character.CharacterId);
        }

        private void GettingNonExistentMiniland()
        {
            Service.GetMiniland(99999);
        }

        private async Task SettingStateToOpen()
        {
            await Service.SetStateAsync(Session.Character.CharacterId, MinilandState.Open);
        }

        private async Task SettingStateToPrivate()
        {
            await Service.SetStateAsync(Session.Character.CharacterId, MinilandState.Private);
        }

        private async Task SettingStateToLock()
        {
            await Service.SetStateAsync(Session.Character.CharacterId, MinilandState.Lock);
        }

        private async Task DeletingMiniland()
        {
            DeleteResult = await Service.DeleteMinilandAsync(Session.Character.CharacterId);
        }

        private async Task DeletingNonExistentMiniland()
        {
            DeleteResult = await Service.DeleteMinilandAsync(99999);
        }

        private void MinilandShouldBeInitialized()
        {
            Assert.IsNotNull(InitializedMiniland);
            Assert.AreNotEqual(Guid.Empty, InitializedMiniland.MapInstanceId);
        }

        private void MinilandShouldBeReturned()
        {
            Assert.IsNotNull(RetrievedMiniland);
            Assert.AreEqual(InitializedMiniland!.MapInstanceId, RetrievedMiniland.MapInstanceId);
        }

        private void ShouldThrowArgumentException_(Lazy<Exception> exception)
        {
            Assert.IsInstanceOfType(exception.Value, typeof(ArgumentException));
        }

        private void StateShouldBeOpen()
        {
            var miniland = Service.GetMiniland(Session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Open, miniland.State);
        }

        private void StateShouldBePrivate()
        {
            var miniland = Service.GetMiniland(Session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Private, miniland.State);
        }

        private void StateShouldBeLock()
        {
            var miniland = Service.GetMiniland(Session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Lock, miniland.State);
        }

        private void DeleteShouldSucceed()
        {
            Assert.IsNotNull(DeleteResult);
        }

        private void DeleteShouldReturnNull()
        {
            Assert.IsNull(DeleteResult);
        }
    }
}

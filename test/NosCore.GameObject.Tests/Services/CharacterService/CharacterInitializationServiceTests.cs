//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.CharacterService
{
    [TestClass]
    public class CharacterInitializationServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private ICharacterInitializationService Service = null!;
        private IMinilandService MinilandService = null!;
        private ClientSession Session = null!;
        private Mock<IFriendHub> FriendHttpClient = null!;

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

            MinilandService = new GameObject.Services.MinilandService.MinilandService(
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

            Service = new CharacterInitializationService(MinilandService, TestHelpers.Instance.MapInstanceGeneratorService);
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

        private ICharacterInitializationService? NullService;
        private ICharacterInitializationService? PartialService;
        private ClientSession? SecondSession;
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
            await NullService!.InitializeAsync(Session.Character);
            InitializationCompleted = true;
        }

        private void ServiceWithOnlyMinilandService()
        {
            PartialService = new CharacterInitializationService(MinilandService);
        }

        private void ServiceWithOnlyMapInstanceService()
        {
            PartialService = new CharacterInitializationService(mapInstanceGeneratorService: TestHelpers.Instance.MapInstanceGeneratorService);
        }

        private async Task InitializingCharacterWithPartialServices()
        {
            await PartialService!.InitializeAsync(Session.Character);
            InitializationCompleted = true;
        }

        private async Task InitializingMultipleCharacters()
        {
            SecondSession = await TestHelpers.Instance.GenerateSessionAsync();
            await TestHelpers.Instance.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto
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
            var miniland2 = MinilandService.GetMiniland(SecondSession!.Character.CharacterId);

            Assert.IsNotNull(miniland1);
            Assert.IsNotNull(miniland2);
            Assert.AreNotEqual(miniland1.MapInstanceId, miniland2.MapInstanceId);
        }
    }
}

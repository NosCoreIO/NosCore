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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MinilandService;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.MinilandService
{
    [TestClass]
    public class MinilandRegistryTests
    {
        private IMinilandRegistry Registry = null!;

        [TestInitialize]
        public void Setup()
        {
            Registry = new MinilandRegistry();
        }

        [TestMethod]
        public async Task RegisterShouldAddMiniland()
        {
            await new Spec("Register should add miniland")
                .Given(MinilandIsCreated)
                .When(RegisteringMiniland)
                .Then(MinilandShouldBeRetrievable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetByCharacterIdShouldReturnNullForUnknownCharacter()
        {
            await new Spec("Get by character ID should return null for unknown character")
                .When(GettingUnknownMiniland)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetByMapInstanceIdShouldReturnMiniland()
        {
            await new Spec("Get by map instance ID should return miniland")
                .Given(MinilandIsRegistered)
                .When(GettingByMapInstanceId)
                .Then(MinilandShouldBeFound)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetByMapInstanceIdShouldReturnNullForUnknownId()
        {
            await new Spec("Get by map instance ID should return null for unknown ID")
                .When(GettingByUnknownMapInstanceId)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisterShouldRemoveMiniland()
        {
            await new Spec("Unregister should remove miniland")
                .Given(MinilandIsRegistered)
                .When(UnregisteringMiniland)
                .Then(UnregisterShouldSucceed)
                .And(MinilandShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ContainsCharacterShouldReturnTrueWhenExists()
        {
            await new Spec("Contains character should return true when exists")
                .Given(MinilandIsRegistered)
                .When(CheckingContainsCharacter)
                .Then(ContainsShouldBeTrue)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ContainsCharacterShouldReturnFalseWhenNotExists()
        {
            await new Spec("Contains character should return false when not exists")
                .When(CheckingContainsUnknownCharacter)
                .Then(ContainsShouldBeFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAllShouldReturnAllMinilands()
        {
            await new Spec("Get all should return all minilands")
                .Given(MultipleMinilandsAreRegistered)
                .When(GettingAllMinilands)
                .Then(AllMinilandsShouldBeReturned)
                .ExecuteAsync();
        }

        private Miniland? TestMiniland;
        private Miniland? ResultMiniland;
        private Miniland? UnregisteredMiniland;
        private bool UnregisterResult;
        private bool ContainsResult;
        private int MinilandCount;
        private readonly Guid TestMapInstanceId = Guid.NewGuid();
        private const long TestCharacterId = 1;
        private const long TestCharacterId2 = 2;

        private void MinilandIsCreated()
        {
            TestMiniland = new Miniland
            {
                MapInstanceId = TestMapInstanceId,
                OwnerId = TestCharacterId
            };
        }

        private void MinilandIsRegistered()
        {
            MinilandIsCreated();
            Registry.Register(TestCharacterId, TestMiniland!);
        }

        private void MultipleMinilandsAreRegistered()
        {
            TestMiniland = new Miniland { MapInstanceId = TestMapInstanceId, OwnerId = TestCharacterId };
            var miniland2 = new Miniland { MapInstanceId = Guid.NewGuid(), OwnerId = TestCharacterId2 };
            Registry.Register(TestCharacterId, TestMiniland);
            Registry.Register(TestCharacterId2, miniland2);
        }

        private void RegisteringMiniland()
        {
            Registry.Register(TestCharacterId, TestMiniland!);
        }

        private void GettingUnknownMiniland()
        {
            ResultMiniland = Registry.GetByCharacterId(9999);
        }

        private void GettingByMapInstanceId()
        {
            ResultMiniland = Registry.GetByMapInstanceId(TestMapInstanceId);
        }

        private void GettingByUnknownMapInstanceId()
        {
            ResultMiniland = Registry.GetByMapInstanceId(Guid.NewGuid());
        }

        private void UnregisteringMiniland()
        {
            UnregisterResult = Registry.Unregister(TestCharacterId, out UnregisteredMiniland);
        }

        private void CheckingContainsCharacter()
        {
            ContainsResult = Registry.ContainsCharacter(TestCharacterId);
        }

        private void CheckingContainsUnknownCharacter()
        {
            ContainsResult = Registry.ContainsCharacter(9999);
        }

        private void GettingAllMinilands()
        {
            MinilandCount = Registry.GetAll().Count();
        }

        private void MinilandShouldBeRetrievable()
        {
            var result = Registry.GetByCharacterId(TestCharacterId);
            Assert.IsNotNull(result);
            Assert.AreEqual(TestMapInstanceId, result.MapInstanceId);
        }

        private void ResultShouldBeNull()
        {
            Assert.IsNull(ResultMiniland);
        }

        private void MinilandShouldBeFound()
        {
            Assert.IsNotNull(ResultMiniland);
            Assert.AreEqual(TestCharacterId, ResultMiniland.OwnerId);
        }

        private void UnregisterShouldSucceed()
        {
            Assert.IsTrue(UnregisterResult);
            Assert.IsNotNull(UnregisteredMiniland);
        }

        private void MinilandShouldNotExist()
        {
            var result = Registry.GetByCharacterId(TestCharacterId);
            Assert.IsNull(result);
        }

        private void ContainsShouldBeTrue()
        {
            Assert.IsTrue(ContainsResult);
        }

        private void ContainsShouldBeFalse()
        {
            Assert.IsFalse(ContainsResult);
        }

        private void AllMinilandsShouldBeReturned()
        {
            Assert.AreEqual(2, MinilandCount);
        }
    }
}

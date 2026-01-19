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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.ShopService;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ShopService
{
    [TestClass]
    public class ShopRegistryTests
    {
        private IShopRegistry Registry = null!;

        [TestInitialize]
        public void Setup()
        {
            Registry = new ShopRegistry();
        }

        [TestMethod]
        public async Task RegisterPlayerShopShouldAddShop()
        {
            await new Spec("Register player shop should add shop")
                .Given(ShopIsCreated)
                .When(RegisteringPlayerShop)
                .Then(ShopShouldBeRetrievable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetPlayerShopShouldReturnNullForUnknownCharacter()
        {
            await new Spec("Get player shop should return null for unknown character")
                .When(GettingUnknownShop)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisterPlayerShopShouldRemoveShop()
        {
            await new Spec("Unregister player shop should remove shop")
                .Given(ShopIsRegistered)
                .When(UnregisteringPlayerShop)
                .Then(ShopShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAllPlayerShopsShouldReturnAllShops()
        {
            await new Spec("Get all player shops should return all shops")
                .Given(MultipleShopsAreRegistered)
                .When(GettingAllShops)
                .Then(AllShopsShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisterPlayerShopShouldOverwriteExisting()
        {
            await new Spec("Register player shop should overwrite existing")
                .Given(ShopIsRegistered)
                .When(RegisteringNewShopForSameCharacter)
                .Then(NewShopShouldReplace)
                .ExecuteAsync();
        }

        private Shop? TestShop;
        private Shop? NewShop;
        private Shop? ResultShop;
        private int ShopCount;
        private const long TestCharacterId = 1;
        private const long TestCharacterId2 = 2;

        private void ShopIsCreated()
        {
            TestShop = new Shop { ShopId = 1 };
        }

        private void ShopIsRegistered()
        {
            ShopIsCreated();
            Registry.RegisterPlayerShop(TestCharacterId, TestShop!);
        }

        private void MultipleShopsAreRegistered()
        {
            TestShop = new Shop { ShopId = 1 };
            NewShop = new Shop { ShopId = 2 };
            Registry.RegisterPlayerShop(TestCharacterId, TestShop);
            Registry.RegisterPlayerShop(TestCharacterId2, NewShop);
        }

        private void RegisteringPlayerShop()
        {
            Registry.RegisterPlayerShop(TestCharacterId, TestShop!);
        }

        private void GettingUnknownShop()
        {
            ResultShop = Registry.GetPlayerShop(9999);
        }

        private void UnregisteringPlayerShop()
        {
            Registry.UnregisterPlayerShop(TestCharacterId);
        }

        private void GettingAllShops()
        {
            ShopCount = Registry.GetAllPlayerShops().Count();
        }

        private void RegisteringNewShopForSameCharacter()
        {
            NewShop = new Shop { ShopId = 99 };
            Registry.RegisterPlayerShop(TestCharacterId, NewShop);
        }

        private void ShopShouldBeRetrievable()
        {
            var result = Registry.GetPlayerShop(TestCharacterId);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.ShopId);
        }

        private void ResultShouldBeNull()
        {
            Assert.IsNull(ResultShop);
        }

        private void ShopShouldNotExist()
        {
            var result = Registry.GetPlayerShop(TestCharacterId);
            Assert.IsNull(result);
        }

        private void AllShopsShouldBeReturned()
        {
            Assert.AreEqual(2, ShopCount);
        }

        private void NewShopShouldReplace()
        {
            var result = Registry.GetPlayerShop(TestCharacterId);
            Assert.IsNotNull(result);
            Assert.AreEqual(99, result.ShopId);
        }
    }
}

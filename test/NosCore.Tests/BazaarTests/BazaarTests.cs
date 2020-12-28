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

using Json.More;
using Json.Patch;
using Json.Pointer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Services.BazaarService;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BazaarController = NosCore.MasterServer.Controllers.BazaarController;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class BazaarTests
    {
        public delegate SaveResult DelegateInsert(ref BazaarItemDto y);

        private BazaarController? _bazaarController;
        private BazaarItemsHolder? _bazaarItemsHolder;
        private Guid _guid;
        private Mock<IDao<BazaarItemDto, long>>? _mockBzDao;
        private Mock<IDao<IItemInstanceDto?, Guid>>? _mockItemDao;


        [TestInitialize]
        public void Setup()
        {
            _guid = Guid.NewGuid();
            _mockBzDao = new Mock<IDao<BazaarItemDto, long>>();
            _mockItemDao = new Mock<IDao<IItemInstanceDto?, Guid>>();

            var mockCharacterDao = new Mock<IDao<CharacterDto, long>>();
            _bazaarItemsHolder =
                new BazaarItemsHolder(_mockBzDao.Object, _mockItemDao.Object, mockCharacterDao.Object);
            _bazaarController = new BazaarController(new BazaarService(_bazaarItemsHolder, _mockBzDao.Object, _mockItemDao.Object));
            _mockItemDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                .Returns<IItemInstanceDto?>(Task.FromResult);
            _mockBzDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<BazaarItemDto>()))
                .Returns<BazaarItemDto>(Task.FromResult);
        }

        [TestMethod]
        public async Task AddToBazaarAllStackAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = await _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 99,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 50
                }).ConfigureAwait(false);
            Assert.AreEqual(_guid, _bazaarItemsHolder?.BazaarItems[0].BazaarItem?.ItemInstanceId);
            Assert.AreEqual(99, _bazaarItemsHolder?.BazaarItems[0].BazaarItem?.Amount ?? 0);
            Assert.AreEqual(LanguageKey.OBJECT_IN_BAZAAR, add);
        }

        [TestMethod]
        public async Task AddToBazaarPartialStackAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = await _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 50,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 50
                }).ConfigureAwait(false);
            Assert.AreNotEqual(_guid, _bazaarItemsHolder!.BazaarItems[0].BazaarItem?.ItemInstanceId);
            Assert.AreEqual(50, _bazaarItemsHolder.BazaarItems[0].BazaarItem?.Amount ?? 0);
            Assert.AreEqual(LanguageKey.OBJECT_IN_BAZAAR, add);
        }

        [TestMethod]
        public async Task AddToBazaarNegativeAmountAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = -50,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 50
                })).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AddToBazaarNegativePriceAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 50,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = -50
                })).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AddToBazaarMoreThanItemAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 100,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 100
                })).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AddToBazaarNullItemAsync()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 50,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 100
                })).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AddMoreThanLimitAsync()
        {
            var rand = new Random();
            _mockBzDao!.Setup(m => m.TryInsertOrUpdateAsync(It.IsAny<BazaarItemDto>()))
                .Returns((BazaarItemDto y) =>
               {
                   y.BazaarItemId = rand.Next(0, 9999999);
                   return Task.FromResult(y);
               });
            LanguageKey? add = null;
            for (var i = 0; i < 12; i++)
            {
                var guid = Guid.NewGuid();
                _mockItemDao.Reset();
                _mockItemDao!.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                    .Returns<IItemInstanceDto?>(Task.FromResult);
                _mockItemDao!
                    .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                    .ReturnsAsync(new ItemInstanceDto
                    {
                        Id = guid,
                        Amount = 99
                    });
                add = await _bazaarController!.AddBazaarAsync(
                    new BazaarRequest
                    {
                        Amount = 99,
                        CharacterId = 1,
                        CharacterName = "test",
                        Duration = 3600,
                        HasMedal = false,
                        IsPackage = false,
                        ItemInstanceId = guid,
                        Price = 50
                    }).ConfigureAwait(false);
            }

            Assert.AreEqual(10, _bazaarItemsHolder!.BazaarItems.Count);
            Assert.AreEqual(LanguageKey.LIMIT_EXCEEDED, add);
        }

        [TestMethod]
        public async Task DeleteFromBazaarNegativeCountAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            await _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 99,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 50
                }).ConfigureAwait(false);
            Assert.AreEqual(false, await _bazaarController.DeleteBazaarAsync(0, -1, "test").ConfigureAwait(false));
        }

        [TestMethod]
        public async Task DeleteFromBazaarMoreThanRegisteredAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            await _bazaarController!.AddBazaarAsync(
                 new BazaarRequest
                 {
                     Amount = 99,
                     CharacterId = 1,
                     CharacterName = "test",
                     Duration = 3600,
                     HasMedal = false,
                     IsPackage = false,
                     ItemInstanceId = _guid,
                     Price = 50
                 }).ConfigureAwait(false);
            Assert.AreEqual(false, await _bazaarController.DeleteBazaarAsync(0, 100, "test").ConfigureAwait(false));
        }

        [TestMethod]
        public async Task DeleteFromBazaarSomeoneElseAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            await _bazaarController!.AddBazaarAsync(
               new BazaarRequest
               {
                   Amount = 99,
                   CharacterId = 1,
                   CharacterName = "test",
                   Duration = 3600,
                   HasMedal = false,
                   IsPackage = false,
                   ItemInstanceId = _guid,
                   Price = 50
               }).ConfigureAwait(false);
            Assert.AreEqual(true, await _bazaarController.DeleteBazaarAsync(0, 99, "test2").ConfigureAwait(false));
            Assert.AreEqual(1, _bazaarItemsHolder!.BazaarItems.Values.Count);
            Assert.AreEqual(0, _bazaarItemsHolder.BazaarItems[0].ItemInstance?.Amount ?? 0);
            Assert.AreEqual(99, _bazaarItemsHolder.BazaarItems[0].BazaarItem?.Amount ?? 0);
        }

        [TestMethod]
        public async Task DeleteFromUserBazaarAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            await _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 99,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 50
                }).ConfigureAwait(false);
            Assert.AreEqual(true, await _bazaarController.DeleteBazaarAsync(0, 99, "test").ConfigureAwait(false));
            Assert.AreEqual(0, _bazaarItemsHolder!.BazaarItems.Values.Count);
        }

        [TestMethod]
        public async Task DeleteFromBazaarNotRegisteredAsync()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _bazaarController!.DeleteBazaarAsync(0, 99, "test")).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModifyBazaarNotRegisteredAsync()
        {
            var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Create<BazaarLink>(o => o.BazaarItem!.Price), 50.AsJsonElement()));
            Assert.IsNull(await _bazaarController!.ModifyBazaarAsync(0, patch).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ModifyBazaarAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            await _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 99,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 80
                }).ConfigureAwait(false);
            var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Create<BazaarLink>(o => o.BazaarItem!.Price), 50.AsJsonElement()));
            Assert.IsNotNull(await _bazaarController.ModifyBazaarAsync(0, patch).ConfigureAwait(false));
            Assert.AreEqual(50, _bazaarItemsHolder?.BazaarItems[0].BazaarItem?.Price);
        }

        [TestMethod]
        public async Task ModifyBazaarAlreadySoldAsync()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = await _bazaarController!.AddBazaarAsync(
                new BazaarRequest
                {
                    Amount = 99,
                    CharacterId = 1,
                    CharacterName = "test",
                    Duration = 3600,
                    HasMedal = false,
                    IsPackage = false,
                    ItemInstanceId = _guid,
                    Price = 50
                }).ConfigureAwait(false);
            _bazaarItemsHolder!.BazaarItems[0].ItemInstance!.Amount--;
            var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Create<BazaarLink>(o => o.BazaarItem!.Price), 10.AsJsonElement()));
            Assert.IsNull(await _bazaarController.ModifyBazaarAsync(0, patch).ConfigureAwait(false));
            Assert.AreEqual(50, _bazaarItemsHolder.BazaarItems[0].BazaarItem?.Price);
        }
    }
}
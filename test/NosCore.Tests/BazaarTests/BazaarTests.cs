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
using System.Linq.Expressions;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class BazaarTests
    {
        public delegate SaveResult DelegateInsert(ref BazaarItemDto y);

        private BazaarController? _bazaarController;
        private BazaarItemsHolder? _bazaarItemsHolder;
        private Guid _guid;
        private Mock<IGenericDao<BazaarItemDto>>? _mockBzDao;
        private Mock<IGenericDao<IItemInstanceDto>>? _mockItemDao;


        [TestInitialize]
        public void Setup()
        {
            _guid = Guid.NewGuid();
            _mockBzDao = new Mock<IGenericDao<BazaarItemDto>>();
            _mockItemDao = new Mock<IGenericDao<IItemInstanceDto>>();

            var mockCharacterDao = new Mock<IGenericDao<CharacterDto>>();
            _bazaarItemsHolder =
                new BazaarItemsHolder(_mockBzDao.Object, _mockItemDao.Object, mockCharacterDao.Object);
            _bazaarController = new BazaarController(_bazaarItemsHolder, _mockBzDao.Object, _mockItemDao.Object);
        }

        [TestMethod]
        public void AddToBazaarAllStack()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = _bazaarController!.AddBazaar(
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
                });
            Assert.AreEqual(_guid, _bazaarItemsHolder?.BazaarItems[0].BazaarItem?.ItemInstanceId);
            Assert.AreEqual(99, _bazaarItemsHolder?.BazaarItems[0].BazaarItem?.Amount ?? 0);
            Assert.AreEqual(LanguageKey.OBJECT_IN_BAZAAR, add);
        }

        [TestMethod]
        public void AddToBazaarPartialStack()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = _bazaarController!.AddBazaar(
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
                });
            Assert.AreNotEqual(_guid, _bazaarItemsHolder!.BazaarItems[0].BazaarItem?.ItemInstanceId);
            Assert.AreEqual(50, _bazaarItemsHolder.BazaarItems[0].BazaarItem?.Amount ?? 0);
            Assert.AreEqual(LanguageKey.OBJECT_IN_BAZAAR, add);
        }

        [TestMethod]
        public void AddToBazaarNegativeAmount()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            Assert.ThrowsException<ArgumentException>(() => _bazaarController!.AddBazaar(
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
                }));
        }

        [TestMethod]
        public void AddToBazaarNegativePrice()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            Assert.ThrowsException<ArgumentException>(() => _bazaarController!.AddBazaar(
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
                }));
        }

        [TestMethod]
        public void AddToBazaarMoreThanItem()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            Assert.ThrowsException<ArgumentException>(() => _bazaarController!.AddBazaar(
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
                }));
        }

        [TestMethod]
        public void AddToBazaarNullItem()
        {
            Assert.ThrowsException<ArgumentException>(() => _bazaarController!.AddBazaar(
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
                }));
        }

        [TestMethod]
        public void AddMoreThanLimit()
        {
            var rand = new Random();
            _mockBzDao!.Setup(m => m.InsertOrUpdate(ref It.Ref<BazaarItemDto>.IsAny))
                .Returns((DelegateInsert)((ref BazaarItemDto y) =>
               {
                   y.BazaarItemId = rand.Next(0, 9999999);
                   return SaveResult.Saved;
               }));
            LanguageKey? add = null;
            for (var i = 0; i < 12; i++)
            {
                var guid = Guid.NewGuid();
                _mockItemDao.Reset();
                _mockItemDao!
                    .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                    .Returns(new ItemInstanceDto
                    {
                        Id = guid,
                        Amount = 99
                    });
                add = _bazaarController!.AddBazaar(
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
                    });
            }

            Assert.AreEqual(10, _bazaarItemsHolder!.BazaarItems.Count);
            Assert.AreEqual(LanguageKey.LIMIT_EXCEEDED, add);
        }

        [TestMethod]
        public void DeleteFromBazaarNegativeCount()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = _bazaarController!.AddBazaar(
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
                });
            Assert.AreEqual(false, _bazaarController.DeleteBazaar(0, -1, "test"));
        }

        [TestMethod]
        public void DeleteFromBazaarMoreThanRegistered()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = _bazaarController!.AddBazaar(
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
                });
            Assert.AreEqual(false, _bazaarController.DeleteBazaar(0, 100, "test"));
        }

        [TestMethod]
        public void DeleteFromBazaarSomeoneElse()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = _bazaarController!.AddBazaar(
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
                });
            Assert.AreEqual(true, _bazaarController.DeleteBazaar(0, 99, "test2"));
            Assert.AreEqual(1, _bazaarItemsHolder!.BazaarItems.Values.Count);
            Assert.AreEqual(0, _bazaarItemsHolder.BazaarItems[0].ItemInstance?.Amount ?? 0);
            Assert.AreEqual(99, _bazaarItemsHolder.BazaarItems[0].BazaarItem?.Amount ?? 0);
        }

        [TestMethod]
        public void DeleteFromUserBazaar()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = _bazaarController!.AddBazaar(
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
                });
            Assert.AreEqual(true, _bazaarController.DeleteBazaar(0, 99, "test"));
            Assert.AreEqual(0, _bazaarItemsHolder!.BazaarItems.Values.Count);
        }

        [TestMethod]
        public void DeleteFromBazaarNotRegistered()
        {
            Assert.ThrowsException<ArgumentException>(() => _bazaarController!.DeleteBazaar(0, 99, "test"));
        }

        [TestMethod]
        public void ModifyBazaarNotRegistered()
        {
            var patch = new JsonPatchDocument<BazaarLink>();
            patch.Replace(link => link.BazaarItem!.Price, 50);
            Assert.IsNull(_bazaarController!.ModifyBazaar(0, patch));
        }

        [TestMethod]
        public void ModifyBazaar()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = _bazaarController!.AddBazaar(
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
                });
            var patch = new JsonPatchDocument<BazaarLink>();
            patch.Replace(link => link.BazaarItem!.Price, 50);
            Assert.IsNotNull(_bazaarController.ModifyBazaar(0, patch));
            Assert.AreEqual(50, _bazaarItemsHolder?.BazaarItems[0].BazaarItem?.Price);
        }

        [TestMethod]
        public void ModifyBazaarAlreadySold()
        {
            _mockItemDao!
                .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(new ItemInstanceDto
                {
                    Id = _guid,
                    Amount = 99
                });
            var add = _bazaarController!.AddBazaar(
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
                });
            _bazaarItemsHolder!.BazaarItems[0].ItemInstance!.Amount--;
            var patch = new JsonPatchDocument<BazaarLink>();
            patch.Replace(link => link.BazaarItem!.Price, 10);
            Assert.IsNull(_bazaarController.ModifyBazaar(0, patch));
            Assert.AreEqual(50, _bazaarItemsHolder.BazaarItems[0].BazaarItem?.Price);
        }
    }
}
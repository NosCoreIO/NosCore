using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.I18N;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;
using System;
using System.Linq.Expressions;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class BazaarTests
    {
        private BazaarItemsHolder _bazaarItemsHolder;
        private BazaarController _bazaarController;
        private Guid _guid;
        private Mock<IGenericDao<IItemInstanceDto>> _mockItemDao;

        [TestInitialize]
        public void Setup()
        {
            _guid = Guid.NewGuid();
            var mockBzDao = new Mock<IGenericDao<BazaarItemDto>>();
            _mockItemDao = new Mock<IGenericDao<IItemInstanceDto>>();
            var mockCharacterDao = new Mock<IGenericDao<CharacterDto>>();
            var itemList = new System.Collections.Generic.List<ItemDto>();
            _bazaarItemsHolder = new BazaarItemsHolder(mockBzDao.Object, _mockItemDao.Object, itemList, mockCharacterDao.Object);
            _bazaarController = new BazaarController(_bazaarItemsHolder, mockBzDao.Object, _mockItemDao.Object);
        }

        [TestMethod]
        public void AddToBazaarAllStack()
        {
            _mockItemDao
                      .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                      .Returns(new ItemInstanceDto
                      {
                          Id = _guid,
                          Amount = 99
                      });
            var add = _bazaarController.AddBazaar(
                new Data.WebApi.BazaarRequest
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
            Assert.AreEqual(_bazaarItemsHolder.BazaarItems[0].BazaarItem.ItemInstanceId, _guid);
            Assert.AreEqual(_bazaarItemsHolder.BazaarItems[0].BazaarItem.Amount, 99);
            Assert.AreEqual(LanguageKey.OBJECT_IN_BAZAAR, add);
        }

        [TestMethod]
        public void AddToBazaarPartialStack()
        {
            _mockItemDao
                      .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                      .Returns(new ItemInstanceDto
                      {
                          Id = _guid,
                          Amount = 99
                      });
            var add = _bazaarController.AddBazaar(
                new Data.WebApi.BazaarRequest
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
            Assert.AreNotEqual(_bazaarItemsHolder.BazaarItems[0].BazaarItem.ItemInstanceId, _guid);
            Assert.AreEqual(_bazaarItemsHolder.BazaarItems[0].BazaarItem.Amount, 50);
            Assert.AreEqual(LanguageKey.OBJECT_IN_BAZAAR, add);
        }

        [TestMethod]
        public void AddToBazaarNegativeAmount()
        {
            _mockItemDao
                       .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                       .Returns(new ItemInstanceDto
                       {
                           Id = _guid,
                           Amount = 99
                       });
            Assert.ThrowsException<ArgumentException>(() => _bazaarController.AddBazaar(
                new Data.WebApi.BazaarRequest
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
            _mockItemDao
                       .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                       .Returns(new ItemInstanceDto
                       {
                           Id = _guid,
                           Amount = 99
                       });
            Assert.ThrowsException<ArgumentException>(() => _bazaarController.AddBazaar(
                new Data.WebApi.BazaarRequest
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
            _mockItemDao
               .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
               .Returns(new ItemInstanceDto
               {
                   Id = _guid,
                   Amount = 99
               });
            Assert.ThrowsException<ArgumentException>(() => _bazaarController.AddBazaar(
                new Data.WebApi.BazaarRequest
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
            Assert.ThrowsException<ArgumentException>(() => _bazaarController.AddBazaar(
                new Data.WebApi.BazaarRequest
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
        public void DeleteFromBazaarNegativeCount()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void DeleteFromBazaarMoreThanRegistered()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void DeleteFromBazaarSomeoneElse()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void DeleteFromUserBazaar()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void DeleteFromBazaarNotRegistered()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void ModifyBazaar()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void ModifyBazaarNotRegistered()
        {
            throw new NotImplementedException();
        }

        //TODO test filters get
    }
}

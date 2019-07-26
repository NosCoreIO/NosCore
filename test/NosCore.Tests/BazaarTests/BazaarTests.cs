using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;
using System;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class BazaarTests
    {
        private BazaarController _bazaarController { get; set; }

        [TestInitialize]
        public void Setup()
        {
            var mockBzDao = new Mock<IGenericDao<BazaarItemDto>>();
            var mockItemDao = new Mock<IGenericDao<IItemInstanceDto>>();
            var mockCharacterDao = new Mock<IGenericDao<CharacterDto>>();
            var itemList = new System.Collections.Generic.List<ItemDto>();
            _bazaarController = new BazaarController(new BazaarItemsHolder(mockBzDao.Object, mockItemDao.Object, itemList, mockCharacterDao.Object), mockBzDao.Object, mockItemDao.Object);
        }

        [TestMethod]
        public void AddToBazaar()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void AddToBazaarLimitExceeded()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void AddToBazaarAllStack()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void AddToBazaarPartialStack()
        {
            throw new NotImplementedException();
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

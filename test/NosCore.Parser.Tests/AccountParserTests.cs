//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Encryption;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Parser.Parsers;
using NosCore.Shared.Enumerations;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class AccountParserTests
    {
        private List<AccountDto> _accounts;
        private AccountParser _parser;

        [TestInitialize]
        public void Setup()
        {
            _accounts = [];
            var accountDao = new Mock<IDao<AccountDto, long>>();
            accountDao.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<AccountDto, bool>>>()))
                .ReturnsAsync((Expression<Func<AccountDto, bool>> predicate) => _accounts.FirstOrDefault(predicate.Compile()));
            accountDao.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<AccountDto>()))
                .Callback<AccountDto>(_accounts.Add)
                .ReturnsAsync((AccountDto account) => account);
            _parser = new AccountParser(accountDao.Object);
        }

        [TestMethod]
        public async Task AnEmptyDatabaseGetsAGameMasterAndAnOrdinaryUser()
        {
            await _parser.InsertAccountsAsync();

            Assert.AreEqual(AuthorityType.GameMaster, _accounts.Single(s => s.Name == "admin").Authority);
            Assert.AreEqual(AuthorityType.User, _accounts.Single(s => s.Name == "test").Authority);
            var password = new Sha512Hasher().Hash("test");
            Assert.IsTrue(_accounts.All(s => s.Password == password));
        }

        [TestMethod]
        public async Task ParsingAgainCreatesNoAccountTwice()
        {
            await _parser.InsertAccountsAsync();

            await _parser.InsertAccountsAsync();

            Assert.AreEqual(2, _accounts.Count);
        }

        [TestMethod]
        public async Task ADatabaseThatOnlyHasTheGameMasterGetsTheUser()
        {
            _accounts.Add(new AccountDto { Name = "admin", Authority = AuthorityType.GameMaster });

            await _parser.InsertAccountsAsync();

            CollectionAssert.AreEquivalent(new[] { "admin", "test" }, _accounts.Select(s => s.Name).ToList());
        }
    }
}

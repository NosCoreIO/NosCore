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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.AuthService;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.AuthService
{
    [TestClass]
    public class AuthCodeServiceTests
    {
        private IAuthCodeService Service = null!;

        [TestInitialize]
        public void Setup()
        {
            Service = new AuthCodeService();
        }

        [TestMethod]
        public async Task StoreAuthCodeShouldAllowRetrieval()
        {
            await new Spec("Store auth code should allow retrieval")
                .When(StoringAuthCode)
                .Then(AuthCodeShouldBeRetrievable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAccountByAuthCodeShouldReturnNullForUnknownCode()
        {
            await new Spec("Get account by auth code should return null for unknown code")
                .When(GettingUnknownAuthCode)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TryRemoveAuthCodeShouldRemoveCode()
        {
            await new Spec("Try remove auth code should remove code")
                .Given(AuthCodeIsStored)
                .When(RemovingAuthCode)
                .Then(RemoveShouldSucceed)
                .And(AuthCodeShouldNoLongerExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TryRemoveAuthCodeShouldFailForUnknownCode()
        {
            await new Spec("Try remove auth code should fail for unknown code")
                .When(RemovingUnknownAuthCode)
                .Then(RemoveShouldFail)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MarkReadyForAuthShouldAllowVerification()
        {
            await new Spec("Mark ready for auth should allow verification")
                .When(MarkingReadyForAuth)
                .Then(IsReadyForAuthShouldReturnTrue)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task IsReadyForAuthShouldReturnFalseForWrongSession()
        {
            await new Spec("Is ready for auth should return false for wrong session")
                .Given(AccountIsMarkedReady)
                .When(CheckingReadyWithWrongSession)
                .Then(WrongSessionCheckShouldReturnFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClearReadyForAuthShouldRemoveReadyState()
        {
            await new Spec("Clear ready for auth should remove ready state")
                .Given(AccountIsMarkedReady)
                .When(ClearingReadyForAuth)
                .Then(IsReadyShouldReturnFalse)
                .ExecuteAsync();
        }

        private const string TestAuthCode = "test-auth-code";
        private const string TestAccountName = "TestAccount";
        private const long TestSessionId = 12345;
        private string? RetrievedAccountName;
        private bool RemoveResult;
        private bool IsReadyResult;

        private void StoringAuthCode()
        {
            Service.StoreAuthCode(TestAuthCode, TestAccountName);
        }

        private void AuthCodeIsStored()
        {
            Service.StoreAuthCode(TestAuthCode, TestAccountName);
        }

        private void AccountIsMarkedReady()
        {
            Service.MarkReadyForAuth(TestAccountName, TestSessionId);
        }

        private void GettingUnknownAuthCode()
        {
            RetrievedAccountName = Service.GetAccountByAuthCode("unknown-code");
        }

        private void RemovingAuthCode()
        {
            RemoveResult = Service.TryRemoveAuthCode(TestAuthCode, out RetrievedAccountName);
        }

        private void RemovingUnknownAuthCode()
        {
            RemoveResult = Service.TryRemoveAuthCode("unknown-code", out RetrievedAccountName);
        }

        private void MarkingReadyForAuth()
        {
            Service.MarkReadyForAuth(TestAccountName, TestSessionId);
        }

        private void CheckingReadyWithWrongSession()
        {
            IsReadyResult = Service.IsReadyForAuth(TestAccountName, 99999);
        }

        private void ClearingReadyForAuth()
        {
            Service.ClearReadyForAuth(TestAccountName);
        }

        private void AuthCodeShouldBeRetrievable()
        {
            var result = Service.GetAccountByAuthCode(TestAuthCode);
            Assert.AreEqual(TestAccountName, result);
        }

        private void ResultShouldBeNull()
        {
            Assert.IsNull(RetrievedAccountName);
        }

        private void RemoveShouldSucceed()
        {
            Assert.IsTrue(RemoveResult);
            Assert.AreEqual(TestAccountName, RetrievedAccountName);
        }

        private void RemoveShouldFail()
        {
            Assert.IsFalse(RemoveResult);
        }

        private void AuthCodeShouldNoLongerExist()
        {
            var result = Service.GetAccountByAuthCode(TestAuthCode);
            Assert.IsNull(result);
        }

        private void IsReadyForAuthShouldReturnTrue()
        {
            var result = Service.IsReadyForAuth(TestAccountName, TestSessionId);
            Assert.IsTrue(result);
        }

        private void WrongSessionCheckShouldReturnFalse()
        {
            Assert.IsFalse(IsReadyResult);
        }

        private void IsReadyShouldReturnFalse()
        {
            var result = Service.IsReadyForAuth(TestAccountName, TestSessionId);
            Assert.IsFalse(result);
        }
    }
}

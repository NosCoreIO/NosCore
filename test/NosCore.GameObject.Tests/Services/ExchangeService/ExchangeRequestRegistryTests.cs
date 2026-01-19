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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.ExchangeService;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ExchangeService
{
    [TestClass]
    public class ExchangeRequestRegistryTests
    {
        private IExchangeRequestRegistry Registry = null!;

        [TestInitialize]
        public void Setup()
        {
            Registry = new ExchangeRequestRegistry();
        }

        [TestMethod]
        public async Task SetExchangeDataShouldAllowRetrieval()
        {
            await new Spec("Set exchange data should allow retrieval")
                .When(SettingExchangeData)
                .Then(ExchangeDataShouldBeRetrievable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetExchangeDataShouldReturnNullForUnknownCharacter()
        {
            await new Spec("Get exchange data should return null for unknown character")
                .When(GettingUnknownExchangeData)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemoveExchangeDataShouldDeleteData()
        {
            await new Spec("Remove exchange data should delete data")
                .Given(ExchangeDataExists)
                .When(RemovingExchangeData)
                .Then(RemoveShouldSucceed)
                .And(ExchangeDataShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SetExchangeRequestShouldTrackBothParties()
        {
            await new Spec("Set exchange request should track both parties")
                .When(SettingExchangeRequest)
                .Then(InitiatorShouldBeTracked)
                .And(TargetShouldBeTracked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetExchangeRequestPairShouldReturnFromInitiator()
        {
            await new Spec("Get exchange request pair should return from initiator")
                .Given(ExchangeRequestExists)
                .When(GettingPairFromInitiator)
                .Then(PairShouldBeCorrect)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetExchangeRequestPairShouldReturnFromTarget()
        {
            await new Spec("Get exchange request pair should return from target")
                .Given(ExchangeRequestExists)
                .When(GettingPairFromTarget)
                .Then(PairShouldBeCorrectFromTarget)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemoveExchangeRequestFromInitiatorShouldRemoveBoth()
        {
            await new Spec("Remove exchange request from initiator should remove both")
                .Given(ExchangeRequestExists)
                .When(RemovingRequestFromInitiator)
                .Then(RequestShouldBeRemoved)
                .And(NeitherPartyShouldHaveExchange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemoveExchangeRequestFromTargetShouldRemoveBoth()
        {
            await new Spec("Remove exchange request from target should remove both")
                .Given(ExchangeRequestExists)
                .When(RemovingRequestFromTarget)
                .Then(RequestShouldBeRemoved)
                .And(NeitherPartyShouldHaveExchange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HasExchangeShouldReturnTrueForActiveExchange()
        {
            await new Spec("Has exchange should return true for active exchange")
                .Given(ExchangeRequestExists)
                .When(CheckingHasExchange)
                .Then(HasExchangeShouldBeTrue)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HasExchangeShouldReturnFalseForNoExchange()
        {
            await new Spec("Has exchange should return false for no exchange")
                .When(CheckingHasExchangeForUnknown)
                .Then(HasExchangeShouldBeFalse)
                .ExecuteAsync();
        }

        private const long TestInitiatorId = 1;
        private const long TestTargetId = 2;
        private ExchangeData? ResultData;
        private KeyValuePair<long, long>? ResultPair;
        private bool RemoveResult;
        private bool HasExchangeResult;

        private void SettingExchangeData()
        {
            Registry.SetExchangeData(TestInitiatorId, new ExchangeData());
        }

        private void ExchangeDataExists()
        {
            Registry.SetExchangeData(TestInitiatorId, new ExchangeData());
        }

        private void ExchangeRequestExists()
        {
            Registry.SetExchangeRequest(TestInitiatorId, TestTargetId);
        }

        private void GettingUnknownExchangeData()
        {
            ResultData = Registry.GetExchangeData(9999);
        }

        private void RemovingExchangeData()
        {
            RemoveResult = Registry.RemoveExchangeData(TestInitiatorId);
        }

        private void SettingExchangeRequest()
        {
            Registry.SetExchangeRequest(TestInitiatorId, TestTargetId);
        }

        private void GettingPairFromInitiator()
        {
            ResultPair = Registry.GetExchangeRequestPair(TestInitiatorId);
        }

        private void GettingPairFromTarget()
        {
            ResultPair = Registry.GetExchangeRequestPair(TestTargetId);
        }

        private void RemovingRequestFromInitiator()
        {
            RemoveResult = Registry.RemoveExchangeRequest(TestInitiatorId);
        }

        private void RemovingRequestFromTarget()
        {
            RemoveResult = Registry.RemoveExchangeRequest(TestTargetId);
        }

        private void CheckingHasExchange()
        {
            HasExchangeResult = Registry.HasExchange(TestInitiatorId);
        }

        private void CheckingHasExchangeForUnknown()
        {
            HasExchangeResult = Registry.HasExchange(9999);
        }

        private void ExchangeDataShouldBeRetrievable()
        {
            var result = Registry.GetExchangeData(TestInitiatorId);
            Assert.IsNotNull(result);
        }

        private void ResultShouldBeNull()
        {
            Assert.IsNull(ResultData);
        }

        private void RemoveShouldSucceed()
        {
            Assert.IsTrue(RemoveResult);
        }

        private void ExchangeDataShouldNotExist()
        {
            var result = Registry.GetExchangeData(TestInitiatorId);
            Assert.IsNull(result);
        }

        private void InitiatorShouldBeTracked()
        {
            var result = Registry.GetExchangeRequest(TestInitiatorId);
            Assert.AreEqual(TestTargetId, result);
        }

        private void TargetShouldBeTracked()
        {
            Assert.IsTrue(Registry.HasExchange(TestTargetId));
        }

        private void PairShouldBeCorrect()
        {
            Assert.IsNotNull(ResultPair);
            Assert.AreEqual(TestInitiatorId, ResultPair.Value.Key);
            Assert.AreEqual(TestTargetId, ResultPair.Value.Value);
        }

        private void PairShouldBeCorrectFromTarget()
        {
            Assert.IsNotNull(ResultPair);
            Assert.AreEqual(TestInitiatorId, ResultPair.Value.Key);
            Assert.AreEqual(TestTargetId, ResultPair.Value.Value);
        }

        private void RequestShouldBeRemoved()
        {
            Assert.IsTrue(RemoveResult);
        }

        private void NeitherPartyShouldHaveExchange()
        {
            Assert.IsFalse(Registry.HasExchange(TestInitiatorId));
            Assert.IsFalse(Registry.HasExchange(TestTargetId));
        }

        private void HasExchangeShouldBeTrue()
        {
            Assert.IsTrue(HasExchangeResult);
        }

        private void HasExchangeShouldBeFalse()
        {
            Assert.IsFalse(HasExchangeResult);
        }
    }
}

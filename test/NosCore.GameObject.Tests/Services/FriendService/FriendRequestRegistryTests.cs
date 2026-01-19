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
using NosCore.GameObject.Services.FriendService;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.FriendService
{
    [TestClass]
    public class FriendRequestRegistryTests
    {
        private IFriendRequestRegistry Registry = null!;

        [TestInitialize]
        public void Setup()
        {
            Registry = new FriendRequestRegistry();
        }

        [TestMethod]
        public async Task RegisterRequestShouldAddRequest()
        {
            await new Spec("Register request should add request")
                .When(RegisteringRequest)
                .Then(RequestShouldBeRetrievable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetRequestsForCharacterShouldReturnRequestsAsSender()
        {
            await new Spec("Get requests for character should return requests as sender")
                .Given(RequestsExist)
                .When(GettingRequestsForSender)
                .Then(SenderRequestsShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetRequestsForCharacterShouldReturnRequestsAsReceiver()
        {
            await new Spec("Get requests for character should return requests as receiver")
                .Given(RequestsExist)
                .When(GettingRequestsForReceiver)
                .Then(ReceiverRequestsShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisterRequestShouldRemoveRequest()
        {
            await new Spec("Unregister request should remove request")
                .Given(RequestIsRegistered)
                .When(UnregisteringRequest)
                .Then(UnregisterShouldSucceed)
                .And(RequestShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisterRequestShouldFailForUnknownRequest()
        {
            await new Spec("Unregister request should fail for unknown request")
                .When(UnregisteringUnknownRequest)
                .Then(UnregisterShouldFail)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetRequestsForCharacterShouldReturnEmptyForNoRequests()
        {
            await new Spec("Get requests for character should return empty for no requests")
                .When(GettingRequestsForUnknownCharacter)
                .Then(NoRequestsShouldBeReturned)
                .ExecuteAsync();
        }

        private readonly Guid TestRequestId = Guid.NewGuid();
        private const long TestSenderId = 1;
        private const long TestReceiverId = 2;
        private int RequestCount;
        private bool UnregisterResult;

        private void RequestsExist()
        {
            Registry.RegisterRequest(TestRequestId, TestSenderId, TestReceiverId);
            Registry.RegisterRequest(Guid.NewGuid(), TestSenderId, 99);
            Registry.RegisterRequest(Guid.NewGuid(), 99, TestReceiverId);
        }

        private void RequestIsRegistered()
        {
            Registry.RegisterRequest(TestRequestId, TestSenderId, TestReceiverId);
        }

        private void RegisteringRequest()
        {
            Registry.RegisterRequest(TestRequestId, TestSenderId, TestReceiverId);
        }

        private void GettingRequestsForSender()
        {
            RequestCount = Registry.GetRequestsForCharacter(TestSenderId).Count();
        }

        private void GettingRequestsForReceiver()
        {
            RequestCount = Registry.GetRequestsForCharacter(TestReceiverId).Count();
        }

        private void GettingRequestsForUnknownCharacter()
        {
            RequestCount = Registry.GetRequestsForCharacter(9999).Count();
        }

        private void UnregisteringRequest()
        {
            UnregisterResult = Registry.UnregisterRequest(TestRequestId);
        }

        private void UnregisteringUnknownRequest()
        {
            UnregisterResult = Registry.UnregisterRequest(Guid.NewGuid());
        }

        private void RequestShouldBeRetrievable()
        {
            var requests = Registry.GetRequestsForCharacter(TestSenderId);
            Assert.AreEqual(1, requests.Count());
            var request = requests.First();
            Assert.AreEqual(TestRequestId, request.Key);
            Assert.AreEqual(TestSenderId, request.Value.SenderId);
            Assert.AreEqual(TestReceiverId, request.Value.ReceiverId);
        }

        private void SenderRequestsShouldBeReturned()
        {
            Assert.AreEqual(2, RequestCount);
        }

        private void ReceiverRequestsShouldBeReturned()
        {
            Assert.AreEqual(2, RequestCount);
        }

        private void NoRequestsShouldBeReturned()
        {
            Assert.AreEqual(0, RequestCount);
        }

        private void UnregisterShouldSucceed()
        {
            Assert.IsTrue(UnregisterResult);
        }

        private void UnregisterShouldFail()
        {
            Assert.IsFalse(UnregisterResult);
        }

        private void RequestShouldNotExist()
        {
            var requests = Registry.GetRequestsForCharacter(TestSenderId);
            Assert.AreEqual(0, requests.Count());
        }
    }
}

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
using Moq;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GuriRunnerService;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.GuriRunnerService
{
    [TestClass]
    public class GuriRunnerServiceTests
    {
        private IGuriRunnerService Service = null!;
        private List<IEventHandler<GuriPacket, GuriPacket>> Handlers = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handlers = new List<IEventHandler<GuriPacket, GuriPacket>>();

            Service = new GameObject.Services.GuriRunnerService.GuriRunnerService(Handlers);
        }

        [TestMethod]
        public void ServiceCanBeConstructed()
        {
            new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .Execute();
        }

        [TestMethod]
        public void GuriLaunchWithNoHandlersShouldComplete()
        {
            new Spec("Guri launch with no handlers should complete")
                .When(LaunchingGuriWithNoHandlers)
                .Then(LaunchShouldComplete)
                .Execute();
        }

        [TestMethod]
        public void GuriLaunchWithMatchingHandlerShouldExecuteHandler()
        {
            new Spec("Guri launch with matching handler should execute handler")
                .Given(HandlerIsRegistered)
                .When(LaunchingGuri)
                .Then(HandlerShouldBeExecuted)
                .Execute();
        }

        [TestMethod]
        public void GuriLaunchWithNonMatchingHandlerShouldNotExecuteHandler()
        {
            new Spec("Guri launch with non-matching handler should not execute handler")
                .Given(NonMatchingHandlerIsRegistered)
                .When(LaunchingGuri)
                .Then(HandlerShouldNotBeExecuted)
                .Execute();
        }

        private bool LaunchCompleted;
        private Mock<IEventHandler<GuriPacket, GuriPacket>>? MockHandler;
        private bool HandlerExecuted;

        private void HandlerIsRegistered()
        {
            MockHandler = new Mock<IEventHandler<GuriPacket, GuriPacket>>();
            MockHandler.Setup(h => h.Condition(It.IsAny<GuriPacket>()))
                .Returns(true);
            MockHandler.Setup(h => h.ExecuteAsync(It.IsAny<RequestData<GuriPacket>>()))
                .Callback(() => HandlerExecuted = true)
                .Returns(Task.CompletedTask);
            Handlers.Add(MockHandler.Object);
            Service = new GameObject.Services.GuriRunnerService.GuriRunnerService(Handlers);
        }

        private void NonMatchingHandlerIsRegistered()
        {
            MockHandler = new Mock<IEventHandler<GuriPacket, GuriPacket>>();
            MockHandler.Setup(h => h.Condition(It.IsAny<GuriPacket>()))
                .Returns(false);
            Handlers.Add(MockHandler.Object);
            Service = new GameObject.Services.GuriRunnerService.GuriRunnerService(Handlers);
        }

        private void LaunchingGuriWithNoHandlers()
        {
            var data = new GuriPacket();
            Service.GuriLaunch(Session, data);
            LaunchCompleted = true;
        }

        private void LaunchingGuri()
        {
            var data = new GuriPacket();
            Service.GuriLaunch(Session, data);
            LaunchCompleted = true;
        }

        private void ServiceShouldNotBeNull()
        {
            Assert.IsNotNull(Service);
        }

        private void LaunchShouldComplete()
        {
            Assert.IsTrue(LaunchCompleted);
        }

        private void HandlerShouldBeExecuted()
        {
            Assert.IsTrue(HandlerExecuted);
        }

        private void HandlerShouldNotBeExecuted()
        {
            MockHandler?.Verify(h => h.ExecuteAsync(It.IsAny<RequestData<GuriPacket>>()), Times.Never);
        }
    }
}

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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NRunService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.NRunService
{
    [TestClass]
    public class NrunServiceTests
    {
        private INrunService Service = null!;
        private List<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>> Handlers = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handlers = new List<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>();

            Service = new NrunService(Handlers);
        }

        [TestMethod]
        public async Task ServiceCanBeConstructed()
        {
            await new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NRunLaunchWithNoHandlersShouldComplete()
        {
            await new Spec("NRun launch with no handlers should complete")
                .WhenAsync(LaunchingNRunWithNoHandlers)
                .Then(LaunchShouldComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NRunLaunchWithMatchingHandlerShouldExecuteHandler()
        {
            await new Spec("NRun launch with matching handler should execute handler")
                .Given(HandlerIsRegistered)
                .WhenAsync(LaunchingNRun)
                .Then(HandlerShouldBeExecuted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NRunLaunchWithNonMatchingHandlerShouldNotExecuteHandler()
        {
            await new Spec("NRun launch with non-matching handler should not execute handler")
                .Given(NonMatchingHandlerIsRegistered)
                .WhenAsync(LaunchingNRun)
                .Then(HandlerShouldNotBeExecuted)
                .ExecuteAsync();
        }

        private bool LaunchCompleted;
        private Mock<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>? MockHandler;
        private bool HandlerExecuted;

        private void HandlerIsRegistered()
        {
            MockHandler = new Mock<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>();
            MockHandler.Setup(h => h.Condition(It.IsAny<Tuple<IAliveEntity, NrunPacket>>()))
                .Returns(true);
            MockHandler.Setup(h => h.ExecuteAsync(It.IsAny<RequestData<Tuple<IAliveEntity, NrunPacket>>>()))
                .Callback(() => HandlerExecuted = true)
                .Returns(Task.CompletedTask);
            Handlers.Add(MockHandler.Object);
            Service = new NrunService(Handlers);
        }

        private void NonMatchingHandlerIsRegistered()
        {
            MockHandler = new Mock<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>();
            MockHandler.Setup(h => h.Condition(It.IsAny<Tuple<IAliveEntity, NrunPacket>>()))
                .Returns(false);
            Handlers.Add(MockHandler.Object);
            Service = new NrunService(Handlers);
        }

        private async Task LaunchingNRunWithNoHandlers()
        {
            var data = Tuple.Create<IAliveEntity, NrunPacket>(Session.Character, new NrunPacket());
            await Service.NRunLaunchAsync(Session, data);
            LaunchCompleted = true;
        }

        private async Task LaunchingNRun()
        {
            var data = Tuple.Create<IAliveEntity, NrunPacket>(Session.Character, new NrunPacket());
            await Service.NRunLaunchAsync(Session, data);
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
            MockHandler?.Verify(h => h.ExecuteAsync(It.IsAny<RequestData<Tuple<IAliveEntity, NrunPacket>>>()), Times.Never);
        }
    }
}

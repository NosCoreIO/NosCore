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
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.PacketHandlerService;
using NosCore.Packets.Attributes;
using NosCore.Packets.ClientPackets.Chat;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.PacketHandlerService
{
    [TestClass]
    public class PacketHandlerRegistryTests
    {
        private IPacketHandlerRegistry Registry = null!;

        [TestInitialize]
        public void Setup()
        {
            Registry = new PacketHandlerRegistry(new List<IPacketHandler>());
        }

        [TestMethod]
        public async Task GetHandlerShouldReturnNullForUnregisteredPacket()
        {
            await new Spec("Get handler should return null for unregistered packet")
                .When(GettingUnregisteredHandler)
                .Then(HandlerShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetPacketAttributeShouldReturnNullForUnregisteredPacket()
        {
            await new Spec("Get packet attribute should return null for unregistered packet")
                .When(GettingUnregisteredAttribute)
                .Then(AttributeShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegistryWithHandlersShouldRegisterHandlers()
        {
            await new Spec("Registry with handlers should register handlers")
                .Given(RegistryWithHandlersIsCreated)
                .When(GettingRegisteredHandler)
                .Then(HandlerShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegistryWithHandlersShouldRegisterAttributes()
        {
            await new Spec("Registry with handlers should register attributes")
                .Given(RegistryWithHandlersIsCreated)
                .When(GettingRegisteredAttribute)
                .Then(AttributeShouldNotBeNull)
                .ExecuteAsync();
        }

        private IPacketHandler? ResultHandler;
        private PacketHeaderAttribute? ResultAttribute;

        private class TestPacketHandler : PacketHandler<WhisperPacket>
        {
            public override Task ExecuteAsync(WhisperPacket packet, ClientSession clientSession)
            {
                return Task.CompletedTask;
            }
        }

        private void RegistryWithHandlersIsCreated()
        {
            Registry = new PacketHandlerRegistry(new List<IPacketHandler> { new TestPacketHandler() });
        }

        private void GettingUnregisteredHandler()
        {
            ResultHandler = Registry.GetHandler(typeof(string));
        }

        private void GettingUnregisteredAttribute()
        {
            ResultAttribute = Registry.GetPacketAttribute(typeof(string));
        }

        private void GettingRegisteredHandler()
        {
            ResultHandler = Registry.GetHandler(typeof(WhisperPacket));
        }

        private void GettingRegisteredAttribute()
        {
            ResultAttribute = Registry.GetPacketAttribute(typeof(WhisperPacket));
        }

        private void HandlerShouldBeNull()
        {
            Assert.IsNull(ResultHandler);
        }

        private void AttributeShouldBeNull()
        {
            Assert.IsNull(ResultAttribute);
        }

        private void HandlerShouldNotBeNull()
        {
            Assert.IsNotNull(ResultHandler);
        }

        private void AttributeShouldNotBeNull()
        {
            Assert.IsNotNull(ResultAttribute);
        }
    }
}

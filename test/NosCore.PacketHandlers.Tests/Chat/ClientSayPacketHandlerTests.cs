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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Chat;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Chat
{
    [TestClass]
    public class ClientSayPacketHandlerTests
    {
        private ClientSayPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new ClientSayPacketHandler();
        }

        [TestMethod]
        public async Task SayMessageShouldExecuteWithoutError()
        {
            await new Spec("Say message should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(SendingSayMessage)
                .Then(HandlerShouldCompleteSuccessfully)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SayMessageShouldNotBeSentBackToSender()
        {
            await new Spec("Say message should not be sent back to sender")
                .Given(CharacterIsOnMap)
                .WhenAsync(SendingSayMessage)
                .Then(SenderShouldNotReceiveOwnMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EmptyMessageShouldBeHandled()
        {
            await new Spec("Empty message should be handled")
                .Given(CharacterIsOnMap)
                .WhenAsync(SendingEmptyMessage)
                .Then(HandlerShouldCompleteSuccessfully)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task SendingSayMessage()
        {
            await Handler.ExecuteAsync(new ClientSayPacket
            {
                Message = "Hello everyone!"
            }, Session);
        }

        private async Task SendingEmptyMessage()
        {
            await Handler.ExecuteAsync(new ClientSayPacket
            {
                Message = ""
            }, Session);
        }

        private void HandlerShouldCompleteSuccessfully()
        {
            // If we reached here, the handler completed without throwing
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void SenderShouldNotReceiveOwnMessage()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}

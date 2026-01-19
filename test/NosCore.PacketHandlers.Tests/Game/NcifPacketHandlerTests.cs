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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class NcifPacketHandlerTests
    {
        private NcifPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            var logger = new Mock<ILogger>().Object;
            Handler = new NcifPacketHandler(
                logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task NcifForPlayerShouldReturnStatInfo()
        {
            await new Spec("Ncif for player should return stat info")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingPlayerStatInfo)
                .Then(StInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForUnknownTypeShouldNotSendPacket()
        {
            await new Spec("Ncif for unknown type should not send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingUnknownTypeStatInfo)
                .Then(NoStInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForNonExistentEntityShouldNotSendPacket()
        {
            await new Spec("Ncif for non existent entity should not send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingNonExistentEntityStatInfo)
                .Then(NoStInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = Session.Channel!.Id,
                SessionId = Session.SessionId,
                Sender = Session,
                AccountName = Session.Account.Name,
                Disconnect = () => Task.CompletedTask,
                CharacterId = Session.Character.CharacterId,
                MapInstanceId = Session.Character.MapInstance.MapInstanceId,
                Character = Session.Character
            });
        }

        private async Task RequestingPlayerStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Player,
                TargetId = Session.Character.VisualId
            }, Session);
        }

        private async Task RequestingUnknownTypeStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = (VisualType)99,
                TargetId = 1
            }, Session);
        }

        private async Task RequestingNonExistentEntityStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Player,
                TargetId = 99999
            }, Session);
        }

        private void StInfoPacketShouldBeSent()
        {
            Assert.IsTrue(Session.LastPackets.Any(p => p is StPacket));
        }

        private void NoStInfoPacketShouldBeSent()
        {
            Assert.IsFalse(Session.LastPackets.Any(p => p is StPacket));
        }
    }
}

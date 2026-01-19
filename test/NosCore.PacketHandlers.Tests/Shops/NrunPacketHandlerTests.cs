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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.NRunService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class NrunPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private NrunPacketHandler _nrunPacketHandler = null!;
        private ClientSession _session = null!;
        private Mock<INrunService> _nrunServiceMock = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _nrunServiceMock = new Mock<INrunService>();
            _nrunPacketHandler = new NrunPacketHandler(
                Logger,
                _nrunServiceMock.Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task NrunWithNullVisualTypeShouldCallNrunService()
        {
            await new Spec("Nrun with null visual type should call NRunService")
                .WhenAsync(ExecutingNrunPacketWithNullType)
                .Then(NrunServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithUnknownVisualTypeShouldNotCallNrunService()
        {
            await new Spec("Nrun with unknown visual type should not call NRunService")
                .WhenAsync(ExecutingNrunPacketWithUnknownType)
                .Then(NrunServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithNonExistentNpcShouldNotCallNrunService()
        {
            await new Spec("Nrun with non-existent NPC should not call NRunService")
                .WhenAsync(ExecutingNrunPacketWithNonExistentNpc)
                .Then(NrunServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithExistingNpcShouldCallNrunService()
        {
            await new Spec("Nrun with existing NPC should call NRunService")
                .Given(NpcExistsOnMap)
                .WhenAsync(ExecutingNrunPacketWithNpcType)
                .Then(NrunServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithExistingPlayerShouldCallNrunService()
        {
            await new Spec("Nrun with existing player should call NRunService")
                .Given(PlayerIsRegistered)
                .WhenAsync(ExecutingNrunPacketWithPlayerType)
                .Then(NrunServiceShouldBeCalled)
                .ExecuteAsync();
        }

        private void NpcExistsOnMap()
        {
            var npc = new NosCore.GameObject.ComponentEntities.Entities.MapNpc(
                TestHelpers.Instance.GenerateItemProvider(),
                Logger,
                TestHelpers.Instance.DistanceCalculator,
                TestHelpers.Instance.Clock);
            npc.MapNpcId = 100;
            npc.MapId = _session.Character.MapInstance.Map.MapId;
            npc.MapX = 1;
            npc.MapY = 1;
            npc.Initialize(new NosCore.Data.StaticEntities.NpcMonsterDto { NpcMonsterVNum = 1 }, null, null, new List<NosCore.Data.StaticEntities.ShopItemDto>());
            _session.Character.MapInstance.LoadNpcs(new List<NosCore.GameObject.ComponentEntities.Entities.MapNpc> { npc });
        }

        private void PlayerIsRegistered()
        {
            TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = _session.Channel!.Id,
                SessionId = _session.SessionId,
                Sender = _session,
                AccountName = _session.Account.Name,
                Disconnect = () => Task.CompletedTask,
                CharacterId = _session.Character.CharacterId,
                MapInstanceId = _session.Character.MapInstance.MapInstanceId,
                Character = _session.Character
            });
        }

        private async Task ExecutingNrunPacketWithNpcType()
        {
            var packet = new NrunPacket
            {
                VisualType = VisualType.Npc,
                VisualId = 100,
                Type = 0
            };
            await _nrunPacketHandler.ExecuteAsync(packet, _session);
        }

        private async Task ExecutingNrunPacketWithPlayerType()
        {
            var packet = new NrunPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.VisualId,
                Type = 0
            };
            await _nrunPacketHandler.ExecuteAsync(packet, _session);
        }

        private async Task ExecutingNrunPacketWithNullType()
        {
            var packet = new NrunPacket
            {
                VisualType = null,
                VisualId = 0,
                Type = 0
            };
            await _nrunPacketHandler.ExecuteAsync(packet, _session);
        }

        private async Task ExecutingNrunPacketWithUnknownType()
        {
            var packet = new NrunPacket
            {
                VisualType = VisualType.Monster,
                VisualId = 1,
                Type = 0
            };
            await _nrunPacketHandler.ExecuteAsync(packet, _session);
        }

        private async Task ExecutingNrunPacketWithNonExistentNpc()
        {
            var packet = new NrunPacket
            {
                VisualType = VisualType.Npc,
                VisualId = 99999,
                Type = 0
            };
            await _nrunPacketHandler.ExecuteAsync(packet, _session);
        }

        private void NrunServiceShouldBeCalled()
        {
            _nrunServiceMock.Verify(
                x => x.NRunLaunchAsync(
                    It.IsAny<ClientSession>(),
                    It.IsAny<Tuple<IAliveEntity, NrunPacket>>()),
                Times.Once);
        }

        private void NrunServiceShouldNotBeCalled()
        {
            _nrunServiceMock.Verify(
                x => x.NRunLaunchAsync(
                    It.IsAny<ClientSession>(),
                    It.IsAny<Tuple<IAliveEntity, NrunPacket>>()),
                Times.Never);
        }
    }
}

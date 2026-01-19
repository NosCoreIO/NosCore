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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class RequestNpcPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private RequestNpcPacketHandler _requestNpcPacketHandler = null!;
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _requestNpcPacketHandler = new RequestNpcPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task RequestNpcWithNonExistentNpcShouldNotThrow()
        {
            await new Spec("Request NPC with non-existent NPC should not throw")
                .WhenAsync(ExecutingRequestNpcPacketWithNonExistentNpc)
                .Then(SessionShouldRemainValid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestNpcWithUnknownVisualTypeShouldNotThrow()
        {
            await new Spec("Request NPC with unknown visual type should not throw")
                .WhenAsync(ExecutingRequestNpcPacketWithUnknownType)
                .Then(SessionShouldRemainValid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestNpcWithExistingNpcShouldNotThrow()
        {
            await new Spec("Request NPC with existing NPC should not throw")
                .Given(NpcWithDialogExistsOnMap)
                .WhenAsync(ExecutingRequestNpcPacketWithNpcType)
                .Then(SessionShouldRemainValid)
                .ExecuteAsync();
        }

        private void NpcWithDialogExistsOnMap()
        {
            var npc = new NosCore.GameObject.ComponentEntities.Entities.MapNpc(
                TestHelpers.Instance.GenerateItemProvider(),
                Logger,
                TestHelpers.Instance.DistanceCalculator,
                TestHelpers.Instance.Clock);
            npc.MapNpcId = 100;
            npc.Dialog = 100;
            npc.MapId = _session.Character.MapInstance.Map.MapId;
            npc.MapX = 1;
            npc.MapY = 1;
            npc.Initialize(new NosCore.Data.StaticEntities.NpcMonsterDto { NpcMonsterVNum = 1 }, null, null, new List<NosCore.Data.StaticEntities.ShopItemDto>());
            _session.Character.MapInstance.Npcs.Add(npc);
        }

        private async Task ExecutingRequestNpcPacketWithNpcType()
        {
            var packet = new RequestNpcPacket
            {
                Type = VisualType.Npc,
                TargetId = 100
            };
            await _requestNpcPacketHandler.ExecuteAsync(packet, _session);
        }

        private async Task ExecutingRequestNpcPacketWithNonExistentNpc()
        {
            var packet = new RequestNpcPacket
            {
                Type = VisualType.Npc,
                TargetId = 99999
            };
            await _requestNpcPacketHandler.ExecuteAsync(packet, _session);
        }

        private async Task ExecutingRequestNpcPacketWithUnknownType()
        {
            var packet = new RequestNpcPacket
            {
                Type = VisualType.Monster,
                TargetId = 1
            };
            await _requestNpcPacketHandler.ExecuteAsync(packet, _session);
        }

        private void SessionShouldRemainValid()
        {
            Assert.IsNotNull(_session.Character);
        }
    }
}

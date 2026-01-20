//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.ComponentEntities.Extensions;
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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            var npc = new NosCore.GameObject.ComponentEntities.Entities.MapNpc();
            npc.MapNpcId = 100;
            npc.MapId = _session.Character.MapInstance.Map.MapId;
            npc.MapX = 1;
            npc.MapY = 1;
            npc.Initialize(new NosCore.Data.StaticEntities.NpcMonsterDto { NpcMonsterVNum = 1 }, null, null, new List<NosCore.Data.StaticEntities.ShopItemDto>(), TestHelpers.Instance.GenerateItemProvider());
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

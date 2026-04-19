//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class NrunPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private NrunPacketHandler _nrunPacketHandler = null!;
        private ClientSession _session = null!;
        private Mock<IMessageBus> _messageBusMock = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _messageBusMock = new Mock<IMessageBus>();
            _nrunPacketHandler = new NrunPacketHandler(
                Logger,
                _messageBusMock.Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task NrunWithNullVisualTypeShouldPublishEvent()
        {
            await new Spec("Nrun with null visual type should publish NrunRequestedEvent")
                .WhenAsync(ExecutingNrunPacketWithNullType)
                .Then(MessageBusShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithUnknownVisualTypeShouldNotPublishEvent()
        {
            await new Spec("Nrun with unknown visual type should not publish event")
                .WhenAsync(ExecutingNrunPacketWithUnknownType)
                .Then(MessageBusShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithNonExistentNpcShouldNotPublishEvent()
        {
            await new Spec("Nrun with non-existent NPC should not publish event")
                .WhenAsync(ExecutingNrunPacketWithNonExistentNpc)
                .Then(MessageBusShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithExistingNpcShouldPublishEvent()
        {
            await new Spec("Nrun with existing NPC should publish event")
                .Given(NpcExistsOnMap)
                .WhenAsync(ExecutingNrunPacketWithNpcType)
                .Then(MessageBusShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithExistingPlayerShouldPublishEvent()
        {
            await new Spec("Nrun with existing player should publish event")
                .Given(PlayerIsRegistered)
                .WhenAsync(ExecutingNrunPacketWithPlayerType)
                .Then(MessageBusShouldBeCalled)
                .ExecuteAsync();
        }

        private void NpcExistsOnMap()
        {
            var npc = new NosCore.Data.Dto.MapNpcDto
            {
                MapNpcId = 100,
                MapId = _session.Character.MapInstance.Map.MapId,
                MapX = 1,
                MapY = 1,
                VNum = 1
            };
            _session.Character.MapInstance.LoadNpcs(
                new List<NosCore.Data.Dto.MapNpcDto> { npc },
                new List<NosCore.Data.StaticEntities.NpcMonsterDto> { new() { NpcMonsterVNum = 1 } });
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
                MapInstanceId = _session.Character.MapInstance.MapInstanceId
            });
        }

        private async Task ExecutingNrunPacketWithNpcType()
        {
            await _nrunPacketHandler.ExecuteAsync(new NrunPacket
            {
                VisualType = VisualType.Npc,
                VisualId = 100,
                Type = 0
            }, _session);
        }

        private async Task ExecutingNrunPacketWithPlayerType()
        {
            await _nrunPacketHandler.ExecuteAsync(new NrunPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.VisualId,
                Type = 0
            }, _session);
        }

        private async Task ExecutingNrunPacketWithNullType()
        {
            await _nrunPacketHandler.ExecuteAsync(new NrunPacket
            {
                VisualType = null,
                VisualId = 0,
                Type = 0
            }, _session);
        }

        private async Task ExecutingNrunPacketWithUnknownType()
        {
            await _nrunPacketHandler.ExecuteAsync(new NrunPacket
            {
                VisualType = VisualType.Monster,
                VisualId = 1,
                Type = 0
            }, _session);
        }

        private async Task ExecutingNrunPacketWithNonExistentNpc()
        {
            await _nrunPacketHandler.ExecuteAsync(new NrunPacket
            {
                VisualType = VisualType.Npc,
                VisualId = 99999,
                Type = 0
            }, _session);
        }

        private void MessageBusShouldBeCalled()
        {
            _messageBusMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<NrunRequestedEvent>(),
                    It.IsAny<DeliveryOptions?>()),
                Times.Once);
        }

        private void MessageBusShouldNotBeCalled()
        {
            _messageBusMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<NrunRequestedEvent>(),
                    It.IsAny<DeliveryOptions?>()),
                Times.Never);
        }
    }
}

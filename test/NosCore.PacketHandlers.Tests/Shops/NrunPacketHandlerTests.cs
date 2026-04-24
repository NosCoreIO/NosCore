//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class NrunPacketHandlerTests
    {
        private static readonly ILogger<NrunPacketHandler> Logger = new Mock<ILogger<NrunPacketHandler>>().Object;
        private NrunPacketHandler _nrunPacketHandler = null!;
        private ClientSession _session = null!;
        private Mock<INrunEventHandler> _fakeHandlerMock = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _fakeHandlerMock = new Mock<INrunEventHandler>();
            _fakeHandlerMock.SetupGet(h => h.Runner).Returns(NrunRunnerType.ChangeClass);
            _fakeHandlerMock
                .Setup(h => h.HandleAsync(It.IsAny<ClientSession>(), It.IsAny<IAliveEntity?>(), It.IsAny<NrunPacket>()))
                .Returns(Task.CompletedTask);
            _nrunPacketHandler = new NrunPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry,
                new[] { _fakeHandlerMock.Object });
        }

        [TestMethod]
        public async Task NrunWithNullVisualTypeInvokesHandler()
        {
            await new Spec("Nrun with null visual type invokes the matching handler")
                .WhenAsync(ExecutingNrunPacketWithNullType)
                .Then(HandlerShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithUnknownVisualTypeDoesNotInvokeHandler()
        {
            await new Spec("Nrun with unknown visual type does not invoke the handler")
                .WhenAsync(ExecutingNrunPacketWithUnknownType)
                .Then(HandlerShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithoutRegisteredRunnerDoesNotInvokeHandler()
        {
            await new Spec("Nrun with a runner no handler declares is silently dropped")
                .WhenAsync(ExecutingNrunPacketWithUnregisteredRunner)
                .Then(HandlerShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithExistingNpcInvokesHandler()
        {
            await new Spec("Nrun with existing NPC invokes the matching handler")
                .Given(NpcExistsOnMap)
                .WhenAsync(ExecutingNrunPacketWithNpcType)
                .Then(HandlerShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NrunWithExistingPlayerInvokesHandler()
        {
            await new Spec("Nrun with existing player invokes the matching handler")
                .Given(PlayerIsRegistered)
                .WhenAsync(ExecutingNrunPacketWithPlayerType)
                .Then(HandlerShouldBeCalled)
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

        private Task ExecutingNrunPacketWithNpcType() => _nrunPacketHandler.ExecuteAsync(
            new NrunPacket { Runner = NrunRunnerType.ChangeClass, VisualType = VisualType.Npc, VisualId = 100, Type = 0 }, _session);

        private Task ExecutingNrunPacketWithPlayerType() => _nrunPacketHandler.ExecuteAsync(
            new NrunPacket { Runner = NrunRunnerType.ChangeClass, VisualType = VisualType.Player, VisualId = _session.Character.VisualId, Type = 0 }, _session);

        private Task ExecutingNrunPacketWithNullType() => _nrunPacketHandler.ExecuteAsync(
            new NrunPacket { Runner = NrunRunnerType.ChangeClass, VisualType = null, VisualId = 0, Type = 0 }, _session);

        private Task ExecutingNrunPacketWithUnknownType() => _nrunPacketHandler.ExecuteAsync(
            new NrunPacket { Runner = NrunRunnerType.ChangeClass, VisualType = VisualType.Monster, VisualId = 1, Type = 0 }, _session);

        private Task ExecutingNrunPacketWithUnregisteredRunner() => _nrunPacketHandler.ExecuteAsync(
            new NrunPacket { Runner = NrunRunnerType.OpenProduction, VisualType = null, VisualId = 0, Type = 0 }, _session);

        private void HandlerShouldBeCalled()
        {
            _fakeHandlerMock.Verify(h => h.HandleAsync(
                It.IsAny<ClientSession>(), It.IsAny<IAliveEntity?>(), It.IsAny<NrunPacket>()), Times.Once);
        }

        private void HandlerShouldNotBeCalled()
        {
            _fakeHandlerMock.Verify(h => h.HandleAsync(
                It.IsAny<ClientSession>(), It.IsAny<IAliveEntity?>(), It.IsAny<NrunPacket>()), Times.Never);
        }
    }
}

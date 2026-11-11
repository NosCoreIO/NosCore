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
using NosCore.Tests.Shared.BDD.Steps;
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
        private NrunPacketHandler _nrunPacketHandler;
        private ClientSession _session;
        private Mock<INrunEventHandler> _fakeHandlerMock;
        private Mock<INrunEventHandler> _npcHandlerMock;
        private RequestNpcPacketHandler _requestNpcPacketHandler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _fakeHandlerMock = new Mock<INrunEventHandler>();
            _fakeHandlerMock.SetupGet(h => h.Runner).Returns(NrunRunnerType.ChangeClass);
            _fakeHandlerMock
                .Setup(h => h.HandleAsync(It.IsAny<ClientSession>(), It.IsAny<IAliveEntity?>(), It.IsAny<NrunPacket>()))
                .Returns(Task.CompletedTask);
            _npcHandlerMock = new Mock<INrunEventHandler>();
            _npcHandlerMock.SetupGet(h => h.Runner).Returns(NrunRunnerType.OpenBankFacilities);
            _npcHandlerMock.SetupGet(h => h.RequiresNpc).Returns(true);
            _npcHandlerMock
                .Setup(h => h.HandleAsync(It.IsAny<ClientSession>(), It.IsAny<IAliveEntity?>(), It.IsAny<NrunPacket>()))
                .Returns(Task.CompletedTask);
            _nrunPacketHandler = new NrunPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry,
                TestHelpers.Instance.NpcDialogService,
                new[] { _fakeHandlerMock.Object, _npcHandlerMock.Object });
            _requestNpcPacketHandler = new RequestNpcPacketHandler(
                new Mock<ILogger<RequestNpcPacketHandler>>().Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry,
                new Mock<Wolverine.IMessageBus>().Object,
                TestHelpers.Instance.NpcDialogService);
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

        [TestMethod]
        public async Task NpcChoiceAfterTalkingToTheNpcNearbyIsHandled()
        {
            await new Spec("A dialog choice sent to the NPC the character is talking to, next to it, is handled")
                .Given(NpcsStandAt_And_, (short)1, (short)1)
                .AndAsync(TheCharacterTalksToNpc_, 100)
                .WhenAsync(TheCharacterPicksAChoiceOfNpc_, 100L)
                .Then(NpcHandlerShouldBeCalledWithNpc_, 100L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcChoiceWithoutTalkingToTheNpcIsIgnored()
        {
            await new Spec("A dialog choice sent to an NPC whose dialog was never opened is ignored")
                .Given(NpcsStandAt_And_, (short)1, (short)1)
                .WhenAsync(TheCharacterPicksAChoiceOfNpc_, 100L)
                .Then(NpcHandlerShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcChoiceAfterWalkingAwayIsIgnored()
        {
            await new Spec("A dialog choice sent after walking away from the NPC is ignored")
                .Given(NpcsStandAt_And_, (short)1, (short)1)
                .AndAsync(TheCharacterTalksToNpc_, 100)
                .And(TheCharacterWalksTo_, (short)30, (short)30)
                .WhenAsync(TheCharacterPicksAChoiceOfNpc_, 100L)
                .Then(NpcHandlerShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcChoiceSentToAnotherNpcIsIgnored()
        {
            await new Spec("A dialog choice sent to another NPC than the one talked to is ignored")
                .Given(NpcsStandAt_And_, (short)1, (short)1)
                .AndAsync(TheCharacterTalksToNpc_, 100)
                .WhenAsync(TheCharacterPicksAChoiceOfNpc_, 101L)
                .Then(NpcHandlerShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcChoiceFromADialogOpenedOnAnotherMapIsIgnored()
        {
            await new Spec("A dialog choice whose dialog was opened on another map is ignored")
                .Given(NpcsStandAt_And_, (short)1, (short)1)
                .AndAsync(TheCharacterTalksToNpc_, 100)
                .And(TheDialogWasLeftOnAnotherMap)
                .WhenAsync(TheCharacterPicksAChoiceOfNpc_, 100L)
                .Then(NpcHandlerShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcChoiceSentToAPlayerIsIgnored()
        {
            await new Spec("A dialog choice naming a player instead of an NPC is ignored")
                .Given(PlayerIsRegistered)
                .WhenAsync(TheCharacterPicksAChoiceNamingAPlayer)
                .Then(NpcHandlerShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TalkingFromTooFarDoesNotOpenTheDialog()
        {
            await new Spec("Talking to an NPC out of reach opens no dialog, so its choices are ignored")
                .Given(NpcsStandAt_And_, (short)20, (short)20)
                .AndAsync(TheCharacterTalksToNpc_, 100)
                .WhenAsync(TheCharacterPicksAChoiceOfNpc_, 100L)
                .Then(NpcHandlerShouldNotBeCalled)
                .ExecuteAsync();
        }

        private void NpcsStandAt_And_(short x, short y) =>
            _session.WithNpcs(
                new NosCore.Data.Dto.MapNpcDto { MapNpcId = 100, MapX = x, MapY = y, Dialog = 1 },
                new NosCore.Data.Dto.MapNpcDto { MapNpcId = 101, MapX = x, MapY = y, Dialog = 1 });

        private Task TheCharacterTalksToNpc_(int npcId) => _requestNpcPacketHandler.ExecuteAsync(
            new RequestNpcPacket { Type = VisualType.Npc, TargetId = npcId }, _session);

        private void TheCharacterWalksTo_(short x, short y) => _session.WalkTo(x, y);

        private void TheDialogWasLeftOnAnotherMap() => _session.WithDialogLeftOnAnotherMap();

        private Task TheCharacterPicksAChoiceOfNpc_(long npcId) => _nrunPacketHandler.ExecuteAsync(
            new NrunPacket { Runner = NrunRunnerType.OpenBankFacilities, VisualType = VisualType.Npc, VisualId = npcId, Type = 0 }, _session);

        private Task TheCharacterPicksAChoiceNamingAPlayer() => _nrunPacketHandler.ExecuteAsync(
            new NrunPacket { Runner = NrunRunnerType.OpenBankFacilities, VisualType = VisualType.Player, VisualId = _session.Character.VisualId, Type = 0 }, _session);

        private void NpcHandlerShouldBeCalledWithNpc_(long npcId) =>
            _npcHandlerMock.Verify(h => h.HandleAsync(_session,
                It.Is<IAliveEntity?>(t => t != null && t.VisualType == VisualType.Npc && t.VisualId == npcId),
                It.IsAny<NrunPacket>()), Times.Once);

        private void NpcHandlerShouldNotBeCalled() =>
            _npcHandlerMock.Verify(h => h.HandleAsync(
                It.IsAny<ClientSession>(), It.IsAny<IAliveEntity?>(), It.IsAny<NrunPacket>()), Times.Never);

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
                ChannelId = _session.Channel.NotNull().Id,
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
            new NrunPacket { Runner = NrunRunnerType.ChangeClass, VisualType = VisualType.Object, VisualId = 1, Type = 0 }, _session);

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

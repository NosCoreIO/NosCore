//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.NRunService.Handlers;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.NRunService.Handlers
{
    [TestClass]
    public class TeleporterHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private TeleporterHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IMapChangeService> MapChangeServiceMock = null!;
        private MapNpc? Npc;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            MapChangeServiceMock = new Mock<IMapChangeService>();
            Handler = new TeleporterHandler(MapChangeServiceMock.Object);
        }

        [TestMethod]
        public void ConditionShouldReturnTrueForTeleportWithValidDialog()
        {
            new Spec("Condition should return true for teleport with valid dialog")
                .Given(NpcWithTeleportDialog)
                .When(CheckingConditionWithTeleportRunner)
                .Then(ConditionShouldBeTrue)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForNonTeleportRunner()
        {
            new Spec("Condition should return false for non-teleport runner")
                .Given(NpcWithTeleportDialog)
                .When(CheckingConditionWithShopRunner)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForInvalidDialog()
        {
            new Spec("Condition should return false for invalid dialog")
                .Given(NpcWithInvalidDialog)
                .When(CheckingConditionWithTeleportRunner)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public async Task TeleportWithEnoughGoldShouldChangeMap()
        {
            await new Spec("Teleport with enough gold should change map")
                .Given(NpcWithTeleportDialog)
                .And(CharacterHasEnoughGold)
                .WhenAsync(ExecutingTeleport)
                .Then(MapChangeShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeleportWithEnoughGoldShouldRemoveGold()
        {
            await new Spec("Teleport with enough gold should remove gold")
                .Given(NpcWithTeleportDialog)
                .And(CharacterHasEnoughGold)
                .WhenAsync(ExecutingTeleport)
                .Then(GoldShouldBeRemoved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeleportWithoutEnoughGoldShouldNotChangeMap()
        {
            await new Spec("Teleport without enough gold should not change map")
                .Given(NpcWithTeleportDialog)
                .And(CharacterHasNoGold)
                .WhenAsync(ExecutingTeleport)
                .Then(MapChangeShouldNotBeCalled)
                .And(ShouldReceiveNotEnoughGoldMessage)
                .ExecuteAsync();
        }

        private bool ConditionResult;
        private long InitialGold;

        private void NpcWithTeleportDialog()
        {
            Npc = new MapNpc();
            Npc.MapNpcId = 1;
            Npc.Dialog = 439;
            Npc.Initialize(new NpcMonsterDto { NpcMonsterVNum = 1 }, null, null, new List<ShopItemDto>(), TestHelpers.Instance.GenerateItemProvider());
        }

        private void NpcWithInvalidDialog()
        {
            Npc = new MapNpc();
            Npc.MapNpcId = 1;
            Npc.Dialog = 1;
            Npc.Initialize(new NpcMonsterDto { NpcMonsterVNum = 1 }, null, null, new List<ShopItemDto>(), TestHelpers.Instance.GenerateItemProvider());
        }

        private void CharacterHasEnoughGold()
        {
            Session.Character.Gold = 5000;
            InitialGold = Session.Character.Gold;
        }

        private void CharacterHasNoGold()
        {
            Session.Character.Gold = 0;
            InitialGold = 0;
        }

        private void CheckingConditionWithTeleportRunner()
        {
            var packet = new NrunPacket { Runner = NrunRunnerType.Teleport };
            ConditionResult = Handler.Condition(new Tuple<IAliveEntity, NrunPacket>(Npc!, packet));
        }

        private void CheckingConditionWithShopRunner()
        {
            var packet = new NrunPacket { Runner = NrunRunnerType.OpenShop };
            ConditionResult = Handler.Condition(new Tuple<IAliveEntity, NrunPacket>(Npc!, packet));
        }

        private async Task ExecutingTeleport()
        {
            var packet = new NrunPacket
            {
                Runner = NrunRunnerType.Teleport,
                Type = 1
            };
            var requestData = new RequestData<Tuple<IAliveEntity, NrunPacket>>(
                Session,
                new Tuple<IAliveEntity, NrunPacket>(Npc!, packet));
            await Handler.ExecuteAsync(requestData);
        }

        private void ConditionShouldBeTrue()
        {
            Assert.IsTrue(ConditionResult);
        }

        private void ConditionShouldBeFalse()
        {
            Assert.IsFalse(ConditionResult);
        }

        private void MapChangeShouldBeCalled()
        {
            MapChangeServiceMock.Verify(
                x => x.ChangeMapAsync(It.IsAny<ClientSession>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>()),
                Times.Once);
        }

        private void MapChangeShouldNotBeCalled()
        {
            MapChangeServiceMock.Verify(
                x => x.ChangeMapAsync(It.IsAny<ClientSession>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>()),
                Times.Never);
        }

        private void GoldShouldBeRemoved()
        {
            Assert.IsTrue(Session.Character.Gold < InitialGold);
        }

        private void ShouldReceiveNotEnoughGoldMessage()
        {
            Assert.IsTrue(Session.LastPackets.Any(p => p is SayiPacket sayi && sayi.Message == Game18NConstString.NotEnoughGold));
        }
    }
}

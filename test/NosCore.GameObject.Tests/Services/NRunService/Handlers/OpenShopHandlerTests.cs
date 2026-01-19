//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NRunService.Handlers;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.NRunService.Handlers
{
    [TestClass]
    public class OpenShopHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private OpenShopEventHandler Handler = null!;
        private ClientSession Session = null!;
        private MapNpc? Npc;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new OpenShopEventHandler();
        }

        [TestMethod]
        public void ConditionShouldReturnTrueForOpenShopWithEntity()
        {
            new Spec("Condition should return true for open shop with entity")
                .Given(NpcExists)
                .When(CheckingConditionWithOpenShopRunner)
                .Then(ConditionShouldBeTrue)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForOpenShopWithNullEntity()
        {
            new Spec("Condition should return false for open shop with null entity")
                .When(CheckingConditionWithNullEntity)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForNonOpenShopRunner()
        {
            new Spec("Condition should return false for non-open shop runner")
                .Given(NpcExists)
                .When(CheckingConditionWithTeleportRunner)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public async Task ExecutingShouldNotThrow()
        {
            await new Spec("Executing open shop should not throw")
                .Given(NpcExists)
                .WhenAsync(ExecutingOpenShop)
                .Then(SessionShouldRemainValid)
                .ExecuteAsync();
        }

        private bool ConditionResult;

        private void NpcExists()
        {
            Npc = new MapNpc(
                TestHelpers.Instance.GenerateItemProvider(),
                Logger,
                TestHelpers.Instance.DistanceCalculator,
                TestHelpers.Instance.Clock);
            Npc.MapNpcId = 1;
            Npc.Initialize(new NpcMonsterDto { NpcMonsterVNum = 1 }, null, null, new List<ShopItemDto>());
        }

        private void CheckingConditionWithOpenShopRunner()
        {
            var packet = new NrunPacket { Runner = NrunRunnerType.OpenShop };
            ConditionResult = Handler.Condition(new Tuple<IAliveEntity, NrunPacket>(Npc!, packet));
        }

        private void CheckingConditionWithNullEntity()
        {
            var packet = new NrunPacket { Runner = NrunRunnerType.OpenShop };
            ConditionResult = Handler.Condition(new Tuple<IAliveEntity, NrunPacket>(null!, packet));
        }

        private void CheckingConditionWithTeleportRunner()
        {
            var packet = new NrunPacket { Runner = NrunRunnerType.Teleport };
            ConditionResult = Handler.Condition(new Tuple<IAliveEntity, NrunPacket>(Npc!, packet));
        }

        private async Task ExecutingOpenShop()
        {
            var packet = new NrunPacket
            {
                Runner = NrunRunnerType.OpenShop,
                VisualType = VisualType.Npc,
                VisualId = Npc!.MapNpcId,
                Type = 0
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

        private void SessionShouldRemainValid()
        {
            Assert.IsNotNull(Session.Character);
        }
    }
}

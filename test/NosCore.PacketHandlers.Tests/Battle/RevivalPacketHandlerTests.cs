//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class RevivalPacketHandlerTests
    {
        private const short SeedOfPowerVNum = 1012;
        private RevivalPacketHandler _handler = null!;
        private ClientSession _session = null!;
        private Mock<IMapChangeService> _mapChangeService = null!;
        private Mock<IRespawnService> _respawnService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _mapChangeService = new Mock<IMapChangeService>();
            _respawnService = new Mock<IRespawnService>();
            _respawnService.Setup(r => r.ResolveRespawnMapTypeId(It.IsAny<short>())).Returns((long?)1);
            _respawnService.Setup(r => r.GetRespawnLocation(It.IsAny<ICharacterEntity>(), It.IsAny<long?>()))
                .Returns(((short)1, (short)50, (short)60));

            _handler = new RevivalPacketHandler(
                new Mock<ILogger<RevivalPacketHandler>>().Object,
                _mapChangeService.Object,
                _respawnService.Object);
        }

        [TestMethod]
        public async Task RevivalWhileAliveIsIgnored()
        {
            await new Spec("Revival packet arriving when character is already alive is a no-op")
                .Given(CharacterIsAlive)
                .WhenAsync(SendingRevivalOfType_, (byte)0)
                .Then(HpShouldRemainAt_, 500)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FreeReviveInPlaceAtLevelTwentyRestoresFullHp()
        {
            await new Spec("Level<=20 revive-in-place (Type 0) restores HP/MP to 100% without consuming seeds")
                .Given(CharacterIsDeadAtLevel_, (byte)20)
                .And(CharacterHasSeedsInInventory_, (short)5)
                .WhenAsync(SendingRevivalOfType_, (byte)0)
                .Then(HpShouldBeFullyRestored)
                .And(SeedStackShouldStillBe_, (short)5)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RevivePastTwentyWithEnoughSeedsConsumesThemAndRestoresHalfHp()
        {
            await new Spec("Level>20 revive-in-place consumes 5 seeds and restores HP/MP to 50%")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasSeedsInInventory_, (short)10)
                .WhenAsync(SendingRevivalOfType_, (byte)0)
                .Then(HpShouldBeRestoredToPercent_, 50)
                .And(SeedStackShouldStillBe_, (short)5)
                .And(FivePowerSeedUsedMessageShouldBeSent)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RevivePastTwentyWithoutSeedsFallsBackToSavePoint()
        {
            await new Spec("Level>20 without enough seeds emits NotEnoughPowerSeed and warps to the save point")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasSeedsInInventory_, (short)2)
                .WhenAsync(SendingRevivalOfType_, (byte)0)
                .Then(NotEnoughPowerSeedMessageShouldBeSent)
                .And(MapChangeShouldHaveBeenCalledWith_, (short)1, (short)50, (short)60)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReviveAtSavePointWarpsToRespawnServiceLocation()
        {
            await new Spec("Type 1 warps the character to the respawn service's resolved location at full HP/MP")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .WhenAsync(SendingRevivalOfType_, (byte)1)
                .Then(MapChangeShouldHaveBeenCalledWith_, (short)1, (short)50, (short)60)
                .And(HpShouldBeFullyRestored)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ArenaReviveWithEnoughGoldDeductsOneHundredGoldAndStaysOnMap()
        {
            await new Spec("Arena revive (Type 2) charges 100 gold and restores full HP/MP without changing map")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasGold_, 5000L)
                .WhenAsync(SendingRevivalOfType_, (byte)2)
                .Then(GoldShouldBe_, 4900L)
                .And(HpShouldBeFullyRestored)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ArenaReviveWithoutEnoughGoldFallsBackToSavePoint()
        {
            await new Spec("Arena revive without 100 gold falls back to the save-point revive at full HP")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasGold_, 50L)
                .WhenAsync(SendingRevivalOfType_, (byte)2)
                .Then(GoldShouldBe_, 50L)
                .And(MapChangeShouldHaveBeenCalledWith_, (short)1, (short)50, (short)60)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnknownRevivalTypeLeavesCharacterDead()
        {
            await new Spec("Unknown revival Type leaves IsAlive=false (matches OpenNos default-case behaviour)")
                .Given(CharacterIsDeadAtLevel_, (byte)10)
                .WhenAsync(SendingRevivalOfType_, (byte)99)
                .Then(CharacterShouldStillBeDead)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        private void CharacterIsAlive()
        {
            _session.Character.IsAlive = true;
            _session.Character.Hp = 500;
            _session.Character.Mp = 500;
        }

        private void CharacterIsDeadAtLevel_(byte level)
        {
            _session.Character.Level = level;
            _session.Character.Hp = 0;
            _session.Character.Mp = 0;
            _session.Character.IsAlive = false;
        }

        private void CharacterHasSeedsInInventory_(short amount)
        {
            var item = new Item
            {
                VNum = SeedOfPowerVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Main,
            };
            var inst = new ItemInstanceForTest(SeedOfPowerVNum) { Amount = amount, Item = item };
            var inv = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            inv.Slot = 0;
            inv.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[inv.ItemInstanceId] = inv;
        }

        private void CharacterHasGold_(long gold)
        {
            _session.Character.Gold = gold;
        }

        private async Task SendingRevivalOfType_(byte type)
        {
            await _handler.ExecuteAsync(new RevivalPacket { Type = type }, _session);
        }

        private void HpShouldRemainAt_(int expected) =>
            Assert.AreEqual(expected, _session.Character.Hp);

        private void HpShouldBeFullyRestored() =>
            Assert.AreEqual(_session.Character.MaxHp, _session.Character.Hp);

        private void HpShouldBeRestoredToPercent_(int percent) =>
            Assert.AreEqual(Math.Max(1, _session.Character.MaxHp * percent / 100), _session.Character.Hp);

        private void SeedStackShouldStillBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(SeedOfPowerVNum));

        private void GoldShouldBe_(long expected) =>
            Assert.AreEqual(expected, _session.Character.Gold);

        private void CharacterShouldStillBeDead() =>
            Assert.IsFalse(_session.Character.IsAlive);

        private void FivePowerSeedUsedMessageShouldBeSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>()
                .Any(p => p.Message == Game18NConstString.FivePowerSeedUsed));

        private void NotEnoughPowerSeedMessageShouldBeSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>()
                .Any(p => p.Message == Game18NConstString.NotEnoughPowerSeed));

        private void MapChangeShouldNotBeCalled() =>
            _mapChangeService.Verify(m => m.ChangeMapAsync(
                It.IsAny<ClientSession>(), It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);

        private void MapChangeShouldHaveBeenCalledWith_(short expectedMapId, short expectedX, short expectedY) =>
            _mapChangeService.Verify(m => m.ChangeMapAsync(
                _session,
                It.Is<short?>(v => v == expectedMapId),
                It.Is<short?>(v => v == expectedX),
                It.Is<short?>(v => v == expectedY)), Times.Once);

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

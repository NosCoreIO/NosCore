//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.GameObject.Ecs;
using System.Linq;
using NosCore.Packets.ServerPackets.Entities;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
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
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Services.LandOfDeathService;
using NosCore.Networking.SessionGroup;
using System.Collections.Generic;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class RevivalPacketHandlerTests
    {
        private TestWorld World;
        private const short SeedOfPowerVNum = 1012;
        private RevivalPacketHandler _handler;
        private ClientSession _session;
        private Mock<IMapChangeService> _mapChangeService;
        private Mock<IRespawnService> _respawnService;
        private Mock<ILandOfDeathService> _landOfDeath = new();
        private BuffService _buffs;
        private readonly List<IPacket> _broadcast = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(1);
            _mapChangeService = new Mock<IMapChangeService>();
            _respawnService = new Mock<IRespawnService>();
            _respawnService.Setup(r => r.ResolveRespawnMapTypeId(It.IsAny<short>())).Returns((long?)1);
            _respawnService.Setup(r => r.GetRespawnLocation(It.IsAny<PlayerComponentBundle>(), It.IsAny<long?>()))
                .Returns(((short)1, (short)50, (short)60));

            var catalog = new Mock<ICardCatalog>();
            catalog.Setup(c => c.GetTriggeredCard(CardTrigger.ArenaRevival)).Returns(RevivalProtectionCard.Card);
            catalog.Setup(c => c.GetCardBCards(RevivalProtectionCard.CardId)).Returns(RevivalProtectionCard.BCards);
            _buffs = new BuffService(World.Clock, catalog.Object, new Mock<IRandomProvider>().Object, new Mock<IDebuffResistanceService>().Object, new Mock<Wolverine.IMessageBus>().Object);

            _handler = new RevivalPacketHandler(
                new Mock<ILogger<RevivalPacketHandler>>().Object,
                _mapChangeService.Object,
                _respawnService.Object,
                _buffs,
                catalog.Object,
                UpgradeMaterials.Catalog(),
                _landOfDeath.Object);
        }

        [TestMethod]
        public async Task ReviveInPlaceInTheLandOfDeathSpendsASaver()
        {
            await new Spec("Type 0 in the Land of Death spends one Saver and revives on the spot at full HP")
                .Given(CharacterIsDeadAtLevel_, (byte)60)
                .And(CharacterIsInTheLandOfDeath)
                .And(CharacterHasSaversInInventory_, (short)2)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InPlace)
                .Then(HpShouldBeFullyRestored)
                .And(SaverStackShouldBe_, (short)1)
                .And(ReturnToEntranceShouldBeCalled_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReviveInPlaceInTheLandOfDeathWithoutASaverReturnsToTheEntrance()
        {
            await new Spec("Type 0 in the Land of Death without a Saver sends the player back to the entrance")
                .Given(CharacterIsDeadAtLevel_, (byte)60)
                .And(CharacterIsInTheLandOfDeath)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InPlace)
                .Then(ReturnToEntranceShouldBeCalled_, 1)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReviveAtSavePointInTheLandOfDeathReturnsToTheEntrance()
        {
            await new Spec("Type 1 in the Land of Death sends the player back to the entrance, not the save point")
                .Given(CharacterIsDeadAtLevel_, (byte)60)
                .And(CharacterIsInTheLandOfDeath)
                .And(CharacterHasSaversInInventory_, (short)2)
                .WhenAsync(SendingRevivalOfType_, RevivalType.AtSavePoint)
                .Then(ReturnToEntranceShouldBeCalled_, 1)
                .And(SaverStackShouldBe_, (short)2)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FreeReviveInPlaceAtLevelTwentyRestoresFullHp()
        {
            await new Spec("Level<=20 revive-in-place (Type 0) restores HP/MP to 100% without consuming seeds")
                .Given(CharacterIsDeadAtLevel_, (byte)20)
                .And(CharacterHasSeedsInInventory_, (short)5)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InPlace)
                .Then(HpShouldBeFullyRestored)
                .And(SeedStackShouldStillBe_, (short)5)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RevivePastTwentyWithEnoughSeedsConsumesThemAndRestoresHalfHp()
        {
            await new Spec("Level>20 revive-in-place consumes 10 seeds and restores HP/MP to 50%")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasSeedsInInventory_, (short)15)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InPlace)
                .Then(HpShouldBeRestoredToPercent_, 50)
                .And(SeedStackShouldStillBe_, (short)5)
                .And(SeedsUsedMessageShouldBeSent)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RevivePastTwentyWithoutSeedsFallsBackToSavePoint()
        {
            await new Spec("Level>20 without enough seeds emits NotEnoughPowerSeed and warps to the save point")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasSeedsInInventory_, (short)9)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InPlace)
                .Then(NotEnoughPowerSeedMessageShouldBeSent)
                .And(MapChangeShouldHaveBeenCalledWith_, (short)1, (short)50, (short)60)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReviveAtSavePointWarpsToRespawnServiceLocation()
        {
            await new Spec("Type 1 warps the character to the respawn service's resolved location at full HP/MP")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .WhenAsync(SendingRevivalOfType_, RevivalType.AtSavePoint)
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
                .And(CharacterIsInAnArena)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InArena)
                .Then(GoldShouldBe_, 4900L)
                .And(HpShouldBeFullyRestored)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ArenaReviveGrantsTheRevivalProtection()
        {
            await new Spec("Arena revive puts card 169 on the fighter and shows its icon to the map")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasGold_, 5000L)
                .And(CharacterIsInAnArena)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InArena)
                .Then(CharacterShouldHoldTheRevivalProtection)
                .And(TheMapShouldSeeTheRevivalProtectionFor_Deciseconds, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedProtectionDoesNotUndoTheArenaRevive()
        {
            await new Spec("If the protection card cannot be applied, the fighter stays revived and the fee stays paid")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasGold_, 5000L)
                .And(CharacterIsInAnArena)
                .And(ApplyingTheProtectionFails)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InArena)
                .Then(CharacterShouldBeAlive)
                .And(HpShouldBeFullyRestored)
                .And(GoldShouldBe_, 4900L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReviveInPlaceGrantsNoProtection()
        {
            await new Spec("Reviving in place outside an arena gives no protection card")
                .Given(CharacterIsDeadAtLevel_, (byte)20)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InPlace)
                .Then(CharacterShouldNotHoldTheRevivalProtection)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReviveAtSavePointGrantsNoProtection()
        {
            await new Spec("Reviving at the save point gives no protection card")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .WhenAsync(SendingRevivalOfType_, RevivalType.AtSavePoint)
                .Then(CharacterShouldNotHoldTheRevivalProtection)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingAnArenaForTheSavePointGrantsNoProtection()
        {
            await new Spec("Declining the arena revive sends the fighter to the save point without protection")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasGold_, 5000L)
                .And(CharacterIsInAnArena)
                .WhenAsync(SendingRevivalOfType_, RevivalType.AtSavePoint)
                .Then(CharacterShouldNotHoldTheRevivalProtection)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ArenaReviveWithoutEnoughGoldFallsBackToSavePoint()
        {
            await new Spec("Arena revive without 100 gold falls back to the save-point revive at full HP")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasGold_, 50L)
                .And(CharacterIsInAnArena)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InArena)
                .Then(GoldShouldBe_, 50L)
                .And(MapChangeShouldHaveBeenCalledWith_, (short)1, (short)50, (short)60)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ArenaReviveHappensWhereTheFighterFell()
        {
            await new Spec("Reviving in an arena puts the fighter back on the cell they fell on, not the spot they entered from")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasGold_, 5000L)
                .And(CharacterIsInAnArena)
                .And(CharacterFellAt_And_WhileTheSavedSpotIs_And_, (short)4, (short)5, (short)1, (short)1)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InArena)
                .Then(TheRevivalShouldTeleportTo_And_, (short)4, (short)5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SeedReviveInsideAnArenaIsRefused()
        {
            await new Spec("A revival 0 sent after an arena fall is ignored, so the arena fee cannot be skipped")
                .Given(CharacterIsDeadAtLevel_, (byte)10)
                .And(CharacterHasGold_, 5000L)
                .And(CharacterIsInAnArena)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InPlace)
                .Then(GoldShouldBe_, 5000L)
                .And(CharacterShouldStillBeDead)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ArenaReviveOutsideAnArenaIsRefused()
        {
            await new Spec("A revival 2 sent outside an arena is ignored: no gold taken, still dead")
                .Given(CharacterIsDeadAtLevel_, (byte)30)
                .And(CharacterHasGold_, 5000L)
                .WhenAsync(SendingRevivalOfType_, RevivalType.InArena)
                .Then(GoldShouldBe_, 5000L)
                .And(CharacterShouldStillBeDead)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnknownRevivalTypeLeavesCharacterDead()
        {
            await new Spec("Unknown revival Type leaves IsAlive=false (default case)")
                .Given(CharacterIsDeadAtLevel_, (byte)10)
                .WhenAsync(SendingRevivalOfType_, (RevivalType)99)
                .Then(CharacterShouldStillBeDead)
                .And(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        private void CharacterFellAt_And_WhileTheSavedSpotIs_And_(short x, short y, short mapX, short mapY)
        {
            _session.Character.PositionX = x;
            _session.Character.PositionY = y;
            _session.Character.MapX = mapX;
            _session.Character.MapY = mapY;
        }

        private void TheRevivalShouldTeleportTo_And_(short x, short y)
        {
            var tp = _broadcast.OfType<TpPacket>().Last();
            Assert.AreEqual(x, tp.X);
            Assert.AreEqual(y, tp.Y);
        }

        private void CharacterIsInAnArena()
        {
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.ArenaInstance;
            var mapSessions = new Mock<ISessionGroup>();
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((packets, _) => _broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(packets => _broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            _session.Character.MapInstance.Sessions = mapSessions.Object;
        }

        private void ApplyingTheProtectionFails()
        {
            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.ApplyAsync(It.IsAny<IAliveEntity>(), It.IsAny<NosCore.Data.StaticEntities.CardDto>(),
                    It.IsAny<IReadOnlyList<NosCore.Data.StaticEntities.BCardDto>>(), It.IsAny<IAliveEntity>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException());
            var catalog = new Mock<ICardCatalog>();
            catalog.Setup(c => c.GetTriggeredCard(CardTrigger.ArenaRevival)).Returns(RevivalProtectionCard.Card);
            catalog.Setup(c => c.GetCardBCards(RevivalProtectionCard.CardId)).Returns(RevivalProtectionCard.BCards);
            _handler = new RevivalPacketHandler(
                new Mock<ILogger<RevivalPacketHandler>>().Object,
                _mapChangeService.Object,
                _respawnService.Object,
                buffs.Object,
                catalog.Object,
                UpgradeMaterials.Catalog(),
                _landOfDeath.Object);
        }

        private void CharacterShouldBeAlive() =>
            Assert.IsTrue(_session.Character.IsAlive);

        private void CharacterShouldHoldTheRevivalProtection() =>
            Assert.IsTrue(_buffs.HasBuff(_session.Character, RevivalProtectionCard.CardId));

        private void CharacterShouldNotHoldTheRevivalProtection() =>
            Assert.IsFalse(_buffs.HasBuff(_session.Character, RevivalProtectionCard.CardId));

        private void TheMapShouldSeeTheRevivalProtectionFor_Deciseconds(int deciseconds)
        {
            var bf = _broadcast.OfType<BfPacket>().Single();
            Assert.AreEqual(_session.Character.VisualId, bf.VisualId);
            Assert.AreEqual(RevivalProtectionCard.CardId, bf.Buff.NotNull().BuffId);
            Assert.AreEqual(deciseconds, bf.Buff.NotNull().Duration);
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
                Material = MaterialType.SeedOfPower,
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

        private void CharacterIsInTheLandOfDeath() =>
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.LodInstance;

        private void CharacterHasSaversInInventory_(short amount)
        {
            var vnum = UpgradeMaterials.VNum(MaterialType.Saver);
            var item = new Item
            {
                VNum = vnum,
                Type = NoscorePocketType.Etc,
                ItemType = ItemType.Main,
                Material = MaterialType.Saver,
            };
            var inst = new ItemInstanceForTest(vnum) { Amount = amount, Item = item };
            var inv = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            inv.Slot = 0;
            inv.Type = NoscorePocketType.Etc;
            _session.Character.InventoryService[inv.ItemInstanceId] = inv;
        }

        private void SaverStackShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(UpgradeMaterials.VNum(MaterialType.Saver)));

        private void ReturnToEntranceShouldBeCalled_(int times) =>
            _landOfDeath.Verify(l => l.ReturnToEntranceAsync(_session), Times.Exactly(times));

        private async Task SendingRevivalOfType_(RevivalType type)
        {
            await _handler.ExecuteAsync(new RevivalPacket { Type = type }, _session);
        }

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

        private void SeedsUsedMessageShouldBeSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>()
                .Any(p => p.Message == Game18NConstString.YouHaveUsed && p.ArgumentType == 2
                    && Convert.ToInt32(p.Game18NArguments[0]) == SeedOfPowerVNum
                    && Convert.ToInt32(p.Game18NArguments[1]) == 10));

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

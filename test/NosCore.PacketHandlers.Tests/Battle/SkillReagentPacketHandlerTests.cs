//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class SkillReagentPacketHandlerTests
    {
        private const short Bandage = 2087;
        private const short CastId = 16;

        private TestWorld World;
        private ClientSession _session;
        private ClientSession _ally;
        private Mock<IBattleService> _battleService;
        private CharacterSkill _skill;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.ItemList.Add(new Item { Type = NoscorePocketType.Etc, VNum = Bandage });
            _session = await World.CreateSessionAsync();
            _ally = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(1);
            _ally.Character.MapInstance = World.GetBaseMap(1);
            _session.Character.Mp = 100;
            World.Freeze<Mock<IEquipmentStatsService>>()
                .Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            _battleService = World.Freeze<Mock<IBattleService>>();
            _battleService.Setup(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(true);
            _battleService.Setup(b => b.HitArea(It.IsAny<IAliveEntity>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(true);
            World.UseReal<ISkillCastService, SkillCastService>();
            _skill = new CharacterSkill
            {
                Skill = new SkillDto { SkillVNum = 238, CastId = CastId, Range = 3, MpCost = 15, Cooldown = 50, SkillType = 1, ItemVNum = Bandage },
            };
            _session.Character.Skills = new ConcurrentDictionary<short, CharacterSkill> { [238] = _skill };
        }

        [TestMethod]
        public async Task ATargetedCastWithoutTheReagentIsRefusedWithTheClientsMessage()
        {
            await new Spec("A skill cast on a player without its reagent is cancelled with the missing-item message and costs nothing")
                .WhenAsync(CastingOnTheAlly)
                .Then(TheMissingItemMessageShouldNameTheBandage)
                .And(ACancelShouldBeSent)
                .And(TheBattleServiceShouldNotBeCalled)
                .And(TheCharacterShouldHave_Mp, 100)
                .And(TheSkillShouldNotBeOnCooldown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATargetedCastWithTheReagentUsesOneUp()
        {
            await new Spec("A skill cast on a player with its reagent goes through and takes one reagent")
                .Given(TheCharacterCarries_Bandages, (short)2)
                .WhenAsync(CastingOnTheAlly)
                .Then(TheBattleServiceShouldBeCalled)
                .And(TheCharacterShouldCarry_Bandages, 1)
                .And(TheBagChangeShouldBeSent)
                .And(NoMissingItemMessageShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACastTheBattleServiceRefusesKeepsTheReagent()
        {
            await new Spec("A skill cast the battle service refuses keeps its reagent")
                .Given(TheCharacterCarries_Bandages, (short)2)
                .And(TheBattleServiceRefuses)
                .WhenAsync(CastingOnTheAlly)
                .Then(TheCharacterShouldCarry_Bandages, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroundCastWithoutTheReagentIsRefusedWithTheClientsMessage()
        {
            await new Spec("A skill cast at a cell without its reagent is cancelled with the missing-item message")
                .WhenAsync(CastingAtACell)
                .Then(TheMissingItemMessageShouldNameTheBandage)
                .And(ACancelShouldBeSent)
                .And(TheAreaShouldNotBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroundCastWithTheReagentUsesOneUp()
        {
            await new Spec("A skill cast at a cell with its reagent goes through and takes one reagent")
                .Given(TheCharacterCarries_Bandages, (short)1)
                .WhenAsync(CastingAtACell)
                .Then(TheCharacterShouldCarry_Bandages, 0)
                .And(TheBagChangeShouldBeSent)
                .ExecuteAsync();
        }

        private void TheCharacterCarries_Bandages(short amount) =>
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(Bandage, amount), _session.Character.CharacterId), NoscorePocketType.Etc, 0);

        private void TheBattleServiceRefuses() =>
            _battleService.Setup(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(false);

        private Task CastingOnTheAlly() =>
            World.Create<UseSkillPacketHandler>().ExecuteAsync(new UseSkillPacket
            {
                CastId = CastId,
                TargetVisualType = VisualType.Player,
                TargetId = _ally.Character.VisualId,
            }, _session);

        private Task CastingAtACell() =>
            World.Create<UseAoeSkillPacketHandler>().ExecuteAsync(new UseAoeSkillPacket
            {
                CastId = CastId,
                MapX = (short)(_session.Character.PositionX + 1),
                MapY = _session.Character.PositionY,
            }, _session);

        private void TheMissingItemMessageShouldNameTheBandage() =>
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.VisualId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.MissingItemForSkill,
                ArgumentType = 2,
                Game18NArguments = { Bandage.ToString() },
            }, _session.LastPackets.OfType<SayiPacket>().Single());

        private void NoMissingItemMessageShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.MissingItemForSkill));

        private void ACancelShouldBeSent() =>
            PacketAssert.AreEqual(new CancelPacket { Type = CancelPacketType.CancelAutoAttack, TargetId = CastId, Unknow = -1 },
                _session.LastPackets.OfType<CancelPacket>().Single());

        private void TheBattleServiceShouldBeCalled() =>
            _battleService.Verify(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()), Times.Once);

        private void TheBattleServiceShouldNotBeCalled() =>
            _battleService.Verify(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()), Times.Never);

        private void TheAreaShouldNotBeHit() =>
            _battleService.Verify(b => b.HitArea(It.IsAny<IAliveEntity>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<HitArguments>()), Times.Never);

        private void TheCharacterShouldHave_Mp(int mp) => Assert.AreEqual(mp, _session.Character.Mp);

        private void TheSkillShouldNotBeOnCooldown() => Assert.IsNull(_skill.LastUse);

        private void TheCharacterShouldCarry_Bandages(int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(Bandage));

        private void TheBagChangeShouldBeSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<IvnPacket>().Any(p => p.Type == PocketType.Etc
                && p.IvnSubPackets.NotNull().Single().Slot == 0));
    }
}

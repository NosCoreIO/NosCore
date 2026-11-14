//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class VehicleHandlerTests
    {
        private TestWorld World;
        private const short BroomVNum = 5319;
        private ClientSession _session;
        private Mock<ITransformationService> _transformationService;
        private VehicleHandler _handler;
        private InventoryItemInstance _vehicle;
        private Mock<IDao<RollGeneratedItemDto, short>> _rolls;
        private Mock<IRandomProvider> _random;
        private const short ScrollVNum = 5214;
        private static readonly Item Scooter = new() { VNum = 1906, Type = NoscorePocketType.Mount, Speed = 20, Morph = 2368 };
        private static readonly Item Carpet = new() { VNum = 1907, Type = NoscorePocketType.Mount, Speed = 20, Morph = 2370 };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _transformationService = World.Freeze<Mock<ITransformationService>>();
            _rolls = World.Freeze<Mock<IDao<RollGeneratedItemDto, short>>>();
            _rolls.Setup(r => r.Where(It.IsAny<Expression<Func<RollGeneratedItemDto, bool>>>()))
                .Returns<Expression<Func<RollGeneratedItemDto, bool>>>(predicate => new List<RollGeneratedItemDto>
                {
                    new() { OriginalItemVNum = ScrollVNum, ItemGeneratedVNum = Scooter.VNum, ItemGeneratedAmount = 1, Probability = 1 },
                    new() { OriginalItemVNum = ScrollVNum, ItemGeneratedVNum = Carpet.VNum, ItemGeneratedAmount = 1, Probability = 1 },
                }.Where(predicate.Compile()));
            World.ItemList.RemoveAll(i => i.VNum == Scooter.VNum || i.VNum == Carpet.VNum);
            World.ItemList.Add(Scooter);
            World.ItemList.Add(Carpet);
            _random = World.Freeze<Mock<IRandomProvider>>();
            _random.Setup(r => r.Next(0, 2)).Returns(1);
            _handler = World.Create<VehicleHandler>();

            var item = new Item
            {
                VNum = BroomVNum,
                ItemType = ItemType.Special,
                Type = NoscorePocketType.Main,
                Effect = ItemEffectType.Vehicle,
            };
            var instance = new ItemInstanceForTest(BroomVNum) { Amount = 1, Item = item };
            _vehicle = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _vehicle.Slot = 0;
            _vehicle.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_vehicle.ItemInstanceId] = _vehicle;
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        public async Task UsingTheVehicleStartsTheBar(byte mode)
        {
            await new Spec("Using a vehicle starts the three-second bar and does not mount yet")
                .WhenAsync(UsingTheVehicleWithMode_, mode)
                .Then(ABarShouldHaveBeenSent)
                .And(TheCharacterShouldNotHaveMounted)
                .ExecuteAsync();
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        public async Task AnImportedMountUsesTheNormalMountingFlow(byte mode)
        {
            _vehicle.ItemInstance.Item.Type = NoscorePocketType.Mount;
            _vehicle.ItemInstance.Item.ItemType = default;
            _vehicle.ItemInstance.Item.Effect = 0;
            _vehicle.ItemInstance.Item.Speed = 20;
            _vehicle.ItemInstance.Item.Morph = 2406;
            _vehicle.Type = NoscorePocketType.Mount;
            await UsingTheVehicleWithMode_(mode);
            ABarShouldHaveBeenSent();
            _SecondsPass(3);
            await UsingTheVehicleWithMode_(2);
            TheCharacterShouldHaveMountedOnce();
        }

        [TestMethod]
        public async Task ModeZeroCannotMountBeforeTheBarEnds()
        {
            await UsingTheVehicleWithMode_(0);
            _SecondsPass(1);
            await UsingTheVehicleWithMode_(2);
            TheCharacterShouldNotHaveMounted();
        }

        [TestMethod]
        public async Task APlayerWithAShopOpenCannotMount()
        {
            _session.Character.Shop = new NosCore.GameObject.Services.ShopService.Shop();
            await UsingTheVehicleWithMode_(1);
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        [TestMethod]
        public async Task ASittingPlayerStandsUpBeforeMounting()
        {
            _session.Character.IsSitting = true;
            await UsingTheVehicleWithMode_(1);
            Assert.IsFalse(_session.Character.IsSitting);
            ABarShouldHaveBeenSent();
        }

        [TestMethod]
        public async Task ModeZeroDismountsAVehicledCharacter()
        {
            _session.Character.IsVehicled = true;
            await UsingTheVehicleWithMode_(0);
            _transformationService.Verify(t => t.RemoveVehicleAsync(_session), Times.Once);
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        public async Task TheBarsFollowUpMounts(byte mode)
        {
            await new Spec("Once the bar has run, its follow-up mounts the vehicle")
                .GivenAsync(UsingTheVehicleWithMode_, mode)
                .And(_SecondsPass, 3)
                .WhenAsync(UsingTheVehicleWithMode_, (byte)2)
                .Then(TheCharacterShouldHaveMounted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFollowUpWithoutTheBarIsIgnored()
        {
            await new Spec("A follow-up sent without the bar having been started does not mount")
                .WhenAsync(UsingTheVehicleWithMode_, (byte)2)
                .Then(TheCharacterShouldNotHaveMounted)
                .ExecuteAsync();
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        public async Task AFollowUpBeforeTheBarEndsIsIgnored(byte mode)
        {
            await new Spec("A follow-up sent before the bar has run its three seconds does not mount")
                .GivenAsync(UsingTheVehicleWithMode_, mode)
                .And(_SecondsPass, 1)
                .WhenAsync(UsingTheVehicleWithMode_, (byte)2)
                .Then(TheCharacterShouldNotHaveMounted)
                .ExecuteAsync();
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        public async Task TheFollowUpOnlyCountsOnce(byte mode)
        {
            await new Spec("A second follow-up after one bar does not mount again")
                .GivenAsync(UsingTheVehicleWithMode_, mode)
                .And(_SecondsPass, 3)
                .AndAsync(UsingTheVehicleWithMode_, (byte)2)
                .WhenAsync(UsingTheVehicleWithMode_, (byte)2)
                .Then(TheCharacterShouldHaveMountedOnce)
                .ExecuteAsync();
        }

        [TestMethod]
        [DataRow((byte)0, (short)2406)]
        [DataRow((byte)20, (short)0)]
        public async Task AMountWithoutSpeedOrMorphIsIgnored(byte speed, short morph)
        {
            _vehicle.ItemInstance.Item.Type = NoscorePocketType.Mount;
            _vehicle.ItemInstance.Item.ItemType = ItemType.Special;
            _vehicle.ItemInstance.Item.Effect = ItemEffectType.Vehicle;
            _vehicle.ItemInstance.Item.Speed = speed;
            _vehicle.ItemInstance.Item.Morph = morph;
            await UsingTheVehicleWithMode_(0);
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
            TheCharacterShouldNotHaveMounted();
            _transformationService.Verify(t => t.RemoveVehicleAsync(_session), Times.Never);
        }

        [TestMethod]
        public async Task AMovementScrollRidesTheDrawnMountAndIsConsumed()
        {
            await new Spec("A Movement Scroll rides the mount it draws, at that mount's speed, and is used up")
                .Given(TheItemIsAScroll_, (byte)0)
                .And(TheScrollIsConsumedOnMount)
                .And(TheCharacterHas_Scrolls, (short)2)
                .WhenAsync(MountingWithTheScroll)
                .Then(TheCharacterShouldRide_Looking_, Carpet.VNum, Carpet)
                .And(TheCharacterShouldHave_Scrolls, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANoviceScrollRidesAtItsOwnSpeedAndIsKept()
        {
            await new Spec("A Novice Mount Scroll rides at its own speed with the drawn mount's look, and is kept")
                .Given(TheItemIsAScroll_, (byte)17)
                .And(TheCharacterHas_Scrolls, (short)1)
                .WhenAsync(MountingWithTheScroll)
                .Then(TheCharacterShouldRide_Looking_, ScrollVNum, Carpet)
                .And(TheCharacterShouldHave_Scrolls, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AScrollAboveItsLevelCeilingIsRefused()
        {
            await new Spec("A scroll with a level ceiling refuses a character above it")
                .Given(TheItemIsAScroll_, (byte)17)
                .And(TheScrollStopsAtLevel_, (byte)69)
                .And(TheCharacterIsLevel_, (byte)70)
                .WhenAsync(UsingTheVehicleWithMode_, (byte)1)
                .Then(TheLevelTooHighMessageShouldBeSent)
                .And(TheCharacterShouldNotHaveMounted)
                .ExecuteAsync();
        }

        private void TheItemIsAScroll_(byte speed)
        {
            _vehicle.ItemInstance.Item = new Item { VNum = ScrollVNum, Type = NoscorePocketType.Mount, Speed = speed, Morph = -1 };
            ((ItemInstanceForTest)_vehicle.ItemInstance).ItemVNum = ScrollVNum;
            _vehicle.Type = NoscorePocketType.Mount;
        }

        private void TheScrollIsConsumedOnMount() => _vehicle.ItemInstance.Item.ConsumedOnMount = true;

        private void TheScrollStopsAtLevel_(byte level) => _vehicle.ItemInstance.Item.LevelMaximum = level;

        private void TheCharacterIsLevel_(byte level) => _session.Character.Level = level;

        private void TheCharacterHas_Scrolls(short amount) => _vehicle.ItemInstance.Amount = amount;

        private async Task MountingWithTheScroll()
        {
            await UsingTheVehicleWithMode_(1);
            _SecondsPass(3);
            await UsingTheVehicleWithMode_(2);
        }

        private void TheCharacterShouldRide_Looking_(short vehicleVNum, Item look) =>
            _transformationService.Verify(t => t.ChangeVehicleAsync(_session, It.Is<ItemDto>(v => v.VNum == vehicleVNum), look), Times.Once);

        private void TheCharacterShouldHave_Scrolls(int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(ScrollVNum));

        private void TheLevelTooHighMessageShouldBeSent()
        {
            Assert.AreEqual(Game18NConstString.LevelTooHigh, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        private void _SecondsPass(int seconds) =>
            World.Clock.AdvanceSeconds(seconds);

        private async Task UsingTheVehicleWithMode_(byte mode)
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = (PocketType)_vehicle.Type,
                Slot = _vehicle.Slot,
                Mode = mode,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _vehicle, packet));
        }

        private void ABarShouldHaveBeenSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<DelayPacket>().Any());

        private void TheCharacterShouldHaveMounted() =>
            _transformationService.Verify(t => t.ChangeVehicleAsync(_session, It.IsAny<ItemDto>(), It.IsAny<ItemDto>()), Times.Once);

        private void TheCharacterShouldHaveMountedOnce() => TheCharacterShouldHaveMounted();

        private void TheCharacterShouldNotHaveMounted() =>
            _transformationService.Verify(t => t.ChangeVehicleAsync(It.IsAny<ClientSession>(), It.IsAny<ItemDto>(), It.IsAny<ItemDto>()), Times.Never);

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

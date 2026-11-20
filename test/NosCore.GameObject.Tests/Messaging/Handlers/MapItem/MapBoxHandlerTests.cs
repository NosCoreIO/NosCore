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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.MapItem
{
    [TestClass]
    public class MapBoxHandlerTests
    {
        private const short RegularBox = 1042;
        private const short NormalWood = 2039;

        private TestWorld World;
        private ClientSession _session;
        private readonly List<RollGeneratedItemDto> _contents = [];
        private long _boxId;
        private short _boxX;
        private short _boxY;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var rolls = new Mock<IDao<RollGeneratedItemDto, short>>();
            rolls.Setup(x => x.Where(It.IsAny<Expression<Func<RollGeneratedItemDto, bool>>>())).Returns(() => _contents);
            World.Inject(rolls.Object);
            World.ItemList.Add(new ItemDto
            {
                VNum = RegularBox, Type = NoscorePocketType.Main, ItemType = ItemType.Map, Effect = ItemEffectType.BoxEffect,
                Name = new I18NString { [RegionType.EN] = "Regular Box" }
            });
            World.ItemList.Add(new ItemDto
            {
                VNum = NormalWood, Type = NoscorePocketType.Etc, ItemType = ItemType.Production,
                Name = new I18NString { [RegionType.EN] = "Normal Wood" }
            });
        }

        [TestMethod]
        public async Task PickingUpABoxOnTheMapStartsItsBar()
        {
            await new Spec("A box lying on the map is not picked up but starts a two-second bar")
                .Given(ABoxHolding_LiesNextToTheCharacter, NormalWood)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheBoxBarIsStarted)
                .And(TheBoxStaysOnTheMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheBarRunningOutOpensTheBox()
        {
            await new Spec("Once its bar has run, the box opens where it lies and its contents go to the bag")
                .Given(ABoxHolding_LiesNextToTheCharacter, NormalWood)
                .AndAsync(TheCharacterPicksItUp)
                .And(TheBarRunsOut)
                .WhenAsync(TheBarSendsItsFollowUp)
                .Then(TheMapIsShownTheBoxOpening)
                .And(TheBagHolds_, NormalWood)
                .And(TheCharacterIsToldTheyReceived_, NormalWood)
                .And(TheBoxIsGoneFromTheMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFollowUpBeforeTheBarRanOutOpensNothing()
        {
            await new Spec("The bar's follow-up sent before the bar has run opens nothing")
                .Given(ABoxHolding_LiesNextToTheCharacter, NormalWood)
                .AndAsync(TheCharacterPicksItUp)
                .WhenAsync(TheBarSendsItsFollowUp)
                .Then(TheBoxStaysOnTheMap)
                .And(TheBagDoesNotHold_, NormalWood)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEmptyBoxSaysSoAndGoes()
        {
            await new Spec("A box holding nothing says it is empty and leaves the map")
                .Given(AnEmptyBoxLiesNextToTheCharacter)
                .AndAsync(TheCharacterPicksItUp)
                .And(TheBarRunsOut)
                .WhenAsync(TheBarSendsItsFollowUp)
                .Then(TheCharacterIsToldTheBoxIsEmpty)
                .And(TheBoxIsGoneFromTheMap)
                .ExecuteAsync();
        }

        private void AnEmptyBoxLiesNextToTheCharacter()
        {
            _boxX = _session.Character.PositionX;
            _boxY = _session.Character.PositionY;
            _boxId = World.MapItemProvider.NotNull().Create(_session.Character.MapInstance,
                World.GenerateItemProvider().Create(RegularBox, 1), _boxX, _boxY, _session.Character.CharacterId).VisualId;
        }

        private void ABoxHolding_LiesNextToTheCharacter(short vNum)
        {
            _contents.Add(new RollGeneratedItemDto
            {
                OriginalItemVNum = RegularBox, ItemGeneratedVNum = vNum, ItemGeneratedAmount = 1, Probability = 1
            });
            AnEmptyBoxLiesNextToTheCharacter();
        }

        private Task TheCharacterPicksItUp() => Pick(0);

        private Task TheBarSendsItsFollowUp() => Pick(1);

        private Task Pick(long isDelayed)
        {
            var box = _session.Character.MapInstance.TryGetMapItem(_boxId);
            return World.Create<MapBoxHandler>().Handle(new MapItemPickedUpEvent(_session, _boxId, RegularBox, 1,
                box?.ItemInstance, new GetPacket
                {
                    PickerType = VisualType.Player, PickerId = _session.Character.CharacterId, VisualId = _boxId, IsDelayed = isDelayed
                }));
        }

        private void TheBarRunsOut() => World.Clock.AdvanceMilliseconds(MapBoxHandler.OpenDelay);

        private void TheBoxBarIsStarted() =>
            PacketAssert.AreEqual(new DelayPacket
            {
                Delay = MapBoxHandler.OpenDelay,
                Type = DelayPacketType.OpeningBox,
                Packet = new GetPacket
                {
                    PickerType = VisualType.Player, PickerId = _session.Character.CharacterId, VisualId = _boxId, IsDelayed = 1
                }
            }, _session.LastPackets.OfType<DelayPacket>().Single());

        private void TheMapIsShownTheBoxOpening()
        {
            var map = _session.Character.MapInstance.LastPackets.ToList();
            PacketAssert.AreEqual(new GroundEffectPacket
            {
                Effect = 194, XYCoordinates = "9999", MapX = (ushort)_boxX, MapY = (ushort)_boxY
            }, map.OfType<GroundEffectPacket>().Single());
            PacketAssert.AreEqual(new ServerGetPacket
            {
                VisualType = VisualType.Player, VisualId = _session.Character.CharacterId, ItemId = _boxId
            }, map.OfType<ServerGetPacket>().Single());
        }

        private void TheCharacterIsToldTheyReceived_(short vNum)
        {
            PacketAssert.AreEqual(new IconPacket
            {
                VisualType = VisualType.Player, VisualId = _session.Character.CharacterId, IconType = 1, IconParameter = vNum
            }, _session.LastPackets.OfType<IconPacket>().Single());
            PacketAssert.AreEqual(new MsgiPacket
            {
                Type = MessageType.Default, Message = Game18NConstString.ItemReceived, ArgumentType = 1, Game18NArguments = { vNum }
            }, _session.LastPackets.OfType<MsgiPacket>().Single());
        }

        private void TheCharacterIsToldTheBoxIsEmpty() =>
            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.BoxIsEmpty },
                _session.LastPackets.OfType<MsgiPacket>().Single());

        private void TheBagHolds_(short vNum) =>
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(vNum));

        private void TheBagDoesNotHold_(short vNum) =>
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(vNum));

        private void TheBoxStaysOnTheMap() =>
            Assert.IsNotNull(_session.Character.MapInstance.TryGetMapItem(_boxId));

        private void TheBoxIsGoneFromTheMap() =>
            Assert.IsNull(_session.Character.MapInstance.TryGetMapItem(_boxId));
    }
}

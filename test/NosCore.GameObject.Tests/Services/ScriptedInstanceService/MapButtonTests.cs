//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Interfaces;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class MapButtonTests
    {
        private const int Button = 7;
        private const short OffVNum = 1000;
        private const short OnVNum = 1045;
        private TestWorld World;
        private ClientSession _session;
        private ScriptedInstanceRun _run;
        private Script _script;
        private MapInstance _room;
        private Mock<IScriptedInstanceService> _instances;
        private MapButtonService _service;
        private readonly Mock<ISessionGroup> _roomSessions = new();

        private sealed class Script : InstanceScript
        {
            private readonly bool _toggles;
            private readonly ButtonKind _kind;
            private readonly bool _removedOnUse;

            public Script(bool toggles, ButtonKind kind, bool removedOnUse)
            {
                _toggles = toggles;
                _kind = kind;
                _removedOnUse = removedOnUse;
                Definition = InstanceDefinitionBuilder.Named(1, "timespace", "timespace").Build();
            }

            public List<int> Used { get; } = new();

            public override void OnFirstEnter(IInstanceRun run) =>
                run.PlaceButton(1, Button, new Spot(2, 2), OffVNum, OnVNum, _toggles, _kind);

            public override void OnButtonUsed(IInstanceRun run, int room, int button)
            {
                Used.Add(button);
                if (_removedOnUse)
                {
                    run.RemoveButton(room, button);
                }
            }
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _instances = World.Freeze<Mock<IScriptedInstanceService>>();
            _service = World.Create<MapButtonService>();
        }

        [TestMethod]
        public async Task PlacingAButtonDrawsIt()
        {
            await new Spec("A placed button is drawn as a map object in its off state")
                .GivenAsync(ARunWithA_Button, false)
                .Then(TheButtonShouldBeDrawnAs_, OffVNum)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PressingAButtonStartsThePullBar()
        {
            await new Spec("Picking a button up starts a two second bar that sends git back")
                .GivenAsync(ARunWithA_Button, false)
                .WhenAsync(ThePlayerPressesTheButton)
                .Then(ThePullBarShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FinishingTheBarPullsTheLever()
        {
            await new Spec("Finishing the bar turns the button on, tells the room and reaches the script")
                .GivenAsync(ARunWithA_Button, false)
                .AndAsync(ThePlayerPressesTheButton)
                .And(TwoSecondsPass)
                .WhenAsync(ThePlayerFinishesTheBar)
                .Then(TheButtonShouldBeDrawnAs_, OnVNum)
                .And(TheRoomShouldBeToldTheLeverWasPulled)
                .And(TheScriptShouldHaveSeen_Pulls, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FinishingEarlyDoesNothing()
        {
            await new Spec("A git sent before the bar has run is ignored")
                .GivenAsync(ARunWithA_Button, false)
                .AndAsync(ThePlayerPressesTheButton)
                .WhenAsync(ThePlayerFinishesTheBar)
                .Then(TheScriptShouldHaveSeen_Pulls, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASingleUseButtonCannotBePulledTwice()
        {
            await new Spec("A button that does not toggle stays on once pulled")
                .GivenAsync(ARunWithA_Button, false)
                .AndAsync(TheButtonIsPulled)
                .WhenAsync(TheButtonIsPulled)
                .Then(TheScriptShouldHaveSeen_Pulls, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AToggleSwitchesBack()
        {
            await new Spec("A toggling button switches back off on the second pull")
                .GivenAsync(ARunWithA_Button, true)
                .AndAsync(TheButtonIsPulled)
                .WhenAsync(TheButtonIsPulled)
                .Then(TheButtonShouldBeDrawnAs_, OffVNum)
                .And(TheScriptShouldHaveSeen_Pulls, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AButtonOutOfReachCannotBePressed()
        {
            await new Spec("A button too far away starts no bar")
                .GivenAsync(ARunWithA_Button, false)
                .And(ThePlayerStandsFarAway)
                .WhenAsync(ThePlayerPressesTheButton)
                .Then(NoBarShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingASculptureSaysNothing()
        {
            await new Spec("Moving a sculpture redraws it without saying a lever has been pulled")
                .GivenAsync(ARunWithA_, ButtonKind.Sculpture)
                .WhenAsync(TheButtonIsPulled)
                .Then(TheButtonShouldBeDrawnAs_, OnVNum)
                .And(TheScriptShouldHaveSeen_Pulls, 1)
                .And(TheRoomShouldNotBeToldTheLeverWasPulled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TakingAKeyPicksItUp()
        {
            await new Spec("A key is picked up by the player who takes it and leaves the map")
                .GivenAsync(ARunWithA_, ButtonKind.Key)
                .WhenAsync(TheButtonIsPulled)
                .Then(TheRoomShouldSeeThePlayerPickTheKeyUp)
                .And(TheButtonShouldBeGone)
                .And(TheRoomShouldNotBeToldTheLeverWasPulled)
                .And(TheScriptShouldHaveSeen_Pulls, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARemovedButtonLeavesTheMap()
        {
            await new Spec("A removed button is taken off the map and cannot be used again")
                .GivenAsync(ARunWithAButtonRemovedOnUse)
                .AndAsync(TheButtonIsPulled)
                .WhenAsync(TheButtonIsPulled)
                .Then(TheButtonShouldBeGone)
                .And(TheRoomShouldBeSentTheButtonsOut)
                .And(TheScriptShouldHaveSeen_Pulls, 1)
                .ExecuteAsync();
        }

        private Task ARunWithA_Button(bool toggles) => StartRunAsync(new Script(toggles, ButtonKind.Lever, false));

        private Task ARunWithA_(ButtonKind kind) => StartRunAsync(new Script(false, kind, false));

        private Task ARunWithAButtonRemovedOnUse() => StartRunAsync(new Script(true, ButtonKind.Lever, true));

        private async Task StartRunAsync(Script script)
        {
            _room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [20, 0, 20, 0, .. new byte[400]]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            _script = script;
            _run = new ScriptedInstanceRun(
                new TimeSpaceRunHost(
                    new GameObject.Services.ScriptedInstanceService.ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, Logic = _script },
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), new Mock<Wolverine.IMessageBus>().Object, World.Clock),
                _script,
                new Dictionary<int, MapInstance> { [1] = _room },
                new Dictionary<short, NpcMonsterDto>(),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, TimeSpan.FromSeconds(30));
            _session.Character.MapInstance = _room;
            _room.Sessions = _roomSessions.Object;
            _session.Character.PositionX = 2;
            _session.Character.PositionY = 3;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, _room.MapInstanceId);
            _instances.Setup(s => s.GetRun(_room.MapInstanceId)).Returns(_run);
            await _run.StartAsync();
        }

        private void ThePlayerStandsFarAway()
        {
            _session.Character.PositionX = 18;
            _session.Character.PositionY = 18;
        }

        private async Task ThePlayerPressesTheButton()
        {
            _session.LastPackets.Clear();
            await _service.PressAsync(_session.Character, Button);
        }

        private void TwoSecondsPass() => World.Clock.Advance(Duration.FromMilliseconds(MapButtonService.PullDelay));

        private Task ThePlayerFinishesTheBar() => _service.UseAsync(_session.Character, Button);

        private async Task TheButtonIsPulled()
        {
            await ThePlayerPressesTheButton();
            TwoSecondsPass();
            await ThePlayerFinishesTheBar();
        }

        private void TheButtonShouldBeDrawnAs_(short vNum)
        {
            var drawn = _room.GetMapItems(RegionType.EN).OfType<InPacket>()
                .Single(packet => packet.VisualType == VisualType.Object && packet.VisualId == Button);
            Assert.AreEqual(vNum.ToString(), drawn.VNum);
        }

        private void ThePullBarShouldBeSent()
        {
            var bar = _session.LastPackets.OfType<DelayPacket>().Single();
            Assert.AreEqual(MapButtonService.PullDelay, bar.Delay);
            Assert.AreEqual(DelayPacketType.Button, bar.Type);
            Assert.AreEqual(Button, ((GitPacket?)bar.Packet)?.ButtonId);
        }

        private void NoBarShouldBeSent()
        {
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        private void TheRoomShouldBeToldTheLeverWasPulled()
        {
            var broadcast = _roomSessions.Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>()).SelectMany(packets => packets);
            Assert.IsTrue(broadcast.OfType<MsgiPacket>().Any(packet => packet.Message == Game18NConstString.LeverHasBeenPulled));
        }

        private void TheRoomShouldNotBeToldTheLeverWasPulled()
        {
            var broadcast = _roomSessions.Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>()).SelectMany(packets => packets);
            Assert.IsFalse(broadcast.OfType<MsgiPacket>().Any(packet => packet.Message == Game18NConstString.LeverHasBeenPulled));
        }

        private void TheRoomShouldSeeThePlayerPickTheKeyUp()
        {
            var broadcast = _roomSessions.Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>()).SelectMany(packets => packets);
            PacketAssert.AreEqual(new ServerGetPacket { VisualType = VisualType.Player, VisualId = _session.Character.VisualId, ItemId = Button },
                broadcast.OfType<ServerGetPacket>().Single());
        }

        private void TheButtonShouldBeGone()
        {
            Assert.IsFalse(_room.Buttons.ContainsKey(Button));
            Assert.IsFalse(_room.GetMapItems(RegionType.EN).OfType<InPacket>()
                .Any(packet => packet.VisualType == VisualType.Object && packet.VisualId == Button));
        }

        private void TheRoomShouldBeSentTheButtonsOut()
        {
            var broadcast = _roomSessions.Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>()).SelectMany(packets => packets).ToList();
            var lastIn = broadcast.FindLastIndex(packet => packet is InPacket { VisualType: VisualType.Object, VisualId: Button });
            var lastOut = broadcast.FindLastIndex(packet => packet is OutPacket { VisualType: VisualType.Object, VisualId: Button });
            Assert.IsTrue(lastOut > lastIn);
        }

        private void TheScriptShouldHaveSeen_Pulls(int pulls)
        {
            Assert.AreEqual(pulls, _script.Used.Count);
        }
    }
}

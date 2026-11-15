//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class TeamStoneHandlerTests
    {
        private const short StoneVNum = 2170;
        private TestWorld World;
        private ClientSession _leader;
        private ClientSession _member;
        private Mock<IMapChangeService> _mapChange;
        private Mock<IScriptedInstanceService> _scripted;
        private TeamStoneHandler _handler;
        private InventoryItemInstance _stone;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _leader = await World.CreateSessionAsync();
            _member = await World.CreateSessionAsync();
            var registry = World.Freeze<Mock<ISessionRegistry>>();
            long leaderId = _leader.Character.CharacterId;
            long memberId = _member.Character.CharacterId;
            registry.Setup(r => r.GetSessionByCharacterId(leaderId)).Returns(_leader);
            registry.Setup(r => r.GetSessionByCharacterId(memberId)).Returns(_member);
            _mapChange = World.Freeze<Mock<IMapChangeService>>();
            _scripted = World.Freeze<Mock<IScriptedInstanceService>>();
            _handler = World.Create<TeamStoneHandler>();
        }

        [TestMethod]
        public async Task TheLeaderAtARaidGateGathersTheTeam()
        {
            MapHasRaidGate(true);
            FormGroup(GroupType.Team);
            PlaceStone();

            await UseAsync();

            Guid gateMap = _leader.Character.MapInstance.MapInstanceId;
            _mapChange.Verify(m => m.ChangeMapInstanceAsync(_member, gateMap, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
            _mapChange.Verify(m => m.ChangeMapInstanceAsync(_leader, gateMap, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
            Assert.AreEqual(0, StonesLeft());
        }

        [TestMethod]
        public async Task AwayFromARaidGateTheStoneIsKept()
        {
            MapHasRaidGate(false);
            FormGroup(GroupType.Team);
            PlaceStone();

            await UseAsync();

            NobodyMoved();
            Assert.AreEqual(1, StonesLeft());
        }

        [TestMethod]
        public async Task AnOrdinaryGroupKeepsTheStone()
        {
            MapHasRaidGate(true);
            FormGroup(GroupType.Group);
            PlaceStone();

            await UseAsync();

            NobodyMoved();
            Assert.AreEqual(1, StonesLeft());
        }

        [TestMethod]
        public async Task AMemberWhoIsNotTheLeaderKeepsTheStone()
        {
            MapHasRaidGate(true);
            FormGroup(GroupType.Team, leaderFirst: false);
            PlaceStone();

            await UseAsync();

            NobodyMoved();
            Assert.AreEqual(1, StonesLeft());
        }

        [TestMethod]
        public async Task AnItemWithoutTheTeamStoneEffectDoesNothing()
        {
            MapHasRaidGate(true);
            FormGroup(GroupType.Team);
            PlaceStone(ItemEffectType.NoEffect);

            await UseAsync();

            NobodyMoved();
            Assert.AreEqual(1, StonesLeft());
        }

        private void MapHasRaidGate(bool hasGate) =>
            _scripted.Setup(s => s.GenerateRaidGates(It.IsAny<short>()))
                .Returns(hasGate ? new List<GpPacket> { new() } : new List<GpPacket>());

        private void FormGroup(GroupType type, bool leaderFirst = true)
        {
            var group = new Group(type, World.SessionGroupFactory);
            if (leaderFirst)
            {
                group.JoinGroup(_leader.Character);
                group.JoinGroup(_member.Character);
            }
            else
            {
                group.JoinGroup(_member.Character);
                group.JoinGroup(_leader.Character);
            }

            _leader.Character.Group = group;
            _member.Character.Group = group;
        }

        private void NobodyMoved() =>
            _mapChange.Verify(m => m.ChangeMapInstanceAsync(It.IsAny<ClientSession>(), It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);

        private void PlaceStone(ItemEffectType effect = ItemEffectType.TeamStone)
        {
            var stone = new ItemInstance(new Item { VNum = StoneVNum, Type = NoscorePocketType.Main, ItemType = ItemType.Magical, Effect = effect })
            {
                Amount = 1,
            };
            _stone = InventoryItemInstance.Create(stone, _leader.Character.CharacterId);
            _stone.Slot = 0;
            _stone.Type = NoscorePocketType.Main;
            _leader.Character.InventoryService[_stone.ItemInstanceId] = _stone;
        }

        private int StonesLeft() => _leader.Character.InventoryService.CountItem(StoneVNum);

        private Task UseAsync() =>
            _handler.Handle(new ItemUsedEvent(_leader, _stone, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _leader.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
            }));
    }
}

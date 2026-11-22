//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MinilandService;

[TestClass]
public class MinilandSignpostServiceTests
{
    private TestWorld World;
    private ClientSession _owner;
    private ClientSession _visitor;
    private MapInstance _nosVille;
    private Miniland _miniland;
    private Mock<IMapChangeService> _mapChange;
    private MinilandSignpostService _service;
    private NpcComponentBundle _signpost;

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        _mapChange = World.Freeze<Mock<IMapChangeService>>();
        var minilands = new MinilandRegistry();
        World.Inject<IMinilandRegistry>(minilands);
        _service = World.Create<MinilandSignpostService>();
        _owner = await World.CreateSessionAsync();
        _visitor = await World.CreateSessionAsync();
        _nosVille = World.GetBaseMap(MinilandSignpostService.NosVilleMapId);
        _owner.Character.MapInstance = _nosVille;
        _miniland = new Miniland { OwnerId = _owner.Character.CharacterId, MapInstanceId = World.MinilandId, State = MinilandState.Open };
        minilands.Register(_owner.Character.CharacterId, _miniland);
        await _service.PlaceAsync(_owner.Character, new NpcMonsterDto { NpcMonsterVNum = 921, MaxHp = 18000 });
        _signpost = _nosVille.EnumerateNpcs().Single();
    }

    [TestMethod]
    public async Task TalkingToASignpostSendsTheVisitorIntoItsOwnersMiniland()
    {
        Assert.IsTrue(await _service.VisitAsync(_visitor, _signpost));

        _mapChange.Verify(s => s.ChangeMapInstanceAsync(_visitor, World.MinilandId, 5, 8), Times.Once);
    }

    [TestMethod]
    public async Task ALockedMinilandTurnsTheVisitorAway()
    {
        _miniland.State = MinilandState.Lock;

        Assert.IsTrue(await _service.VisitAsync(_visitor, _signpost));

        PacketAssert.AreEqual(new InfoiPacket { Message = Game18NConstString.MinilandLocked },
            _visitor.LastPackets.OfType<InfoiPacket>().Single());
        NoOneMoved();
    }

    [TestMethod]
    public async Task APrivateMinilandTurnsAwayWhoIsNoFriendOfItsOwner()
    {
        _miniland.State = MinilandState.Private;

        Assert.IsTrue(await _service.VisitAsync(_visitor, _signpost));

        PacketAssert.AreEqual(new InfoiPacket { Message = Game18NConstString.MinilandIsPrivate },
            _visitor.LastPackets.OfType<InfoiPacket>().Single());
        NoOneMoved();
    }

    [TestMethod]
    public async Task APrivateMinilandLetsAFriendOfItsOwnerIn()
    {
        _miniland.State = MinilandState.Private;
        var ownerId = _owner.Character.CharacterId;
        World.FriendHub.Setup(s => s.GetFriendsAsync(ownerId)).ReturnsAsync(new List<CharacterRelationStatus>
        {
            new() { CharacterId = _visitor.Character.CharacterId, RelationType = CharacterRelationType.Friend },
        });

        await _service.VisitAsync(_visitor, _signpost);

        _mapChange.Verify(s => s.ChangeMapInstanceAsync(_visitor, World.MinilandId, 5, 8), Times.Once);
    }

    [TestMethod]
    public async Task AnyOtherNpcIsTalkedToAsUsual()
    {
        var npc = _nosVille.PlaceNpc(new NpcMonsterDto { NpcMonsterVNum = 1 }, 1, 1);

        Assert.IsFalse(await _service.VisitAsync(_visitor, npc));
        NoOneMoved();
    }

    [TestMethod]
    public async Task TheSignpostComesDownWhenItsOwnerLeaves()
    {
        var visualId = _signpost.VisualId;
        World.Inject<IMinilandSignpostService>(_service);

        await World.Create<SignpostDisconnectHandler>().HandleDisconnectAsync(_owner);

        Assert.IsFalse(_nosVille.EnumerateNpcs().Any());
        Assert.IsNull(_service.TimeLeft(_owner.Character.CharacterId));
        PacketAssert.AreEqual(new DiePacket { VisualType = VisualType.Npc, VisualId = visualId, TargetVisualType = VisualType.Npc, TargetId = visualId },
            _nosVille.LastPackets.OfType<DiePacket>().Single());
        PacketAssert.AreEqual(new OutPacket { VisualType = VisualType.Npc, VisualId = visualId },
            _nosVille.LastPackets.OfType<OutPacket>().Single());
    }

    private void NoOneMoved() =>
        _mapChange.Verify(s => s.ChangeMapInstanceAsync(It.IsAny<ClientSession>(), It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
}

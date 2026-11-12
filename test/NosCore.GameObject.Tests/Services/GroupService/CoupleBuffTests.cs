//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.GroupService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.GroupService
{
    [TestClass]
    public class CoupleBuffTests
    {
        private const short CoupleCardId = 319;
        private TestWorld World;
        private ClientSession _husband;
        private ClientSession _wife;
        private ClientSession _friend;
        private BuffService _buffs;
        private CoupleBuffService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _husband = await World.CreateSessionAsync();
            _wife = await World.CreateSessionAsync();
            _friend = await World.CreateSessionAsync();

            var husbandId = _husband.Character.CharacterId;
            var wifeId = _wife.Character.CharacterId;
            var friends = World.Freeze<Mock<IFriendHub>>();
            friends.Setup(s => s.GetFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>());
            friends.Setup(s => s.GetFriendsAsync(husbandId))
                .ReturnsAsync([Relation(_wife, CharacterRelationType.Spouse), Relation(_friend, CharacterRelationType.Friend)]);
            friends.Setup(s => s.GetFriendsAsync(wifeId))
                .ReturnsAsync([Relation(_husband, CharacterRelationType.Spouse)]);
            var cards = World.Freeze<Mock<ICardCatalog>>();
            var couple = new CardDto { CardId = CoupleCardId, Trigger = CardTrigger.SpouseInGroup };
            cards.Setup(s => s.GetCard(CoupleCardId)).Returns(couple);
            cards.Setup(s => s.GetTriggeredCard(CardTrigger.SpouseInGroup)).Returns(couple);
            cards.Setup(s => s.GetCardBCards(CoupleCardId)).Returns(new List<BCardDto>());

            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _service = World.Create<CoupleBuffService>();
        }

        private static CharacterRelationStatus Relation(ClientSession other, CharacterRelationType type) =>
            new() { CharacterId = other.Character.CharacterId, RelationType = type };

        private ClientSession[] Everyone => [_husband, _wife, _friend];

        private void Group(params ClientSession[] members)
        {
            var group = new Group(GroupType.Group, World.SessionGroupFactory);
            foreach (var member in members)
            {
                member.Character.JoinGroup(group);
            }
        }

        private Task RefreshAsync() => _service.RefreshAsync(Everyone.Select(s => (PlayerComponentBundle)s.Character));

        private bool HasBuff(ClientSession session) => _buffs.HasBuff(session.Character, CoupleCardId);

        [TestMethod]
        public async Task SpousesInOneGroupBothGetTheBuff()
        {
            Group(_husband, _wife, _friend);

            await RefreshAsync();

            Assert.IsTrue(HasBuff(_husband));
            Assert.IsTrue(HasBuff(_wife));
            Assert.IsFalse(HasBuff(_friend));
            var vb = _husband.LastPackets.OfType<VbPacket>().Single();
            Assert.AreEqual(CoupleCardId, vb.CardId);
            Assert.IsTrue(vb.IsActivated);
        }

        [TestMethod]
        public async Task AFriendIsNotASpouse()
        {
            Group(_husband, _friend);

            await RefreshAsync();

            Assert.IsFalse(HasBuff(_husband));
            Assert.IsFalse(HasBuff(_friend));
        }

        [TestMethod]
        public async Task LeavingTheGroupTakesTheBuffOffBoth()
        {
            Group(_husband, _wife, _friend);
            await RefreshAsync();

            await _wife.Character.LeaveGroupAsync(World.SessionGroupFactory, World.SessionRegistry);
            await RefreshAsync();

            Assert.IsFalse(HasBuff(_husband));
            Assert.IsFalse(HasBuff(_wife));
            Assert.IsFalse(_wife.LastPackets.OfType<VbPacket>().Last().IsActivated);
        }

        [TestMethod]
        public async Task TheBuffOutlivesDeath()
        {
            Group(_husband, _wife);
            await RefreshAsync();

            await _buffs.ClearAsync(_husband.Character);

            Assert.IsTrue(HasBuff(_husband));
        }
    }
}

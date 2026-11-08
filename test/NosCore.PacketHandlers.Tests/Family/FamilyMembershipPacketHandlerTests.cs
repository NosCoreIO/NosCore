//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.PacketHandlers.Family;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyDismissPacket = NosCore.Packets.ClientPackets.Commands.FamilyDismissPacket;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyMembershipPacketHandlerTests
    {
        private delegate bool TryGetCharacter(Func<PlayerComponentBundle, bool> predicate, out PlayerComponentBundle character);

        private const long FamilyId = 42;
        private ClientSession _head = null!;
        private ClientSession _other = null!;
        private Mock<ISessionRegistry> _sessionRegistry = null!;
        private Mock<IFamilyService> _familyService = null!;
        private Mock<IPubSubHub> _pubSubHub = null!;
        private Mock<IBlacklistHub> _blacklistHub = null!;
        private readonly List<IMessage> _published = new();
        private long _otherId;
        private string _otherName = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _head = await TestHelpers.Instance.GenerateSessionAsync();
            _other = await TestHelpers.Instance.GenerateSessionAsync();
            _other.Character.Family = null;
            _otherId = _other.Character.CharacterId;
            _otherName = _other.Character.Name;
            var characters = new[] { _head.Character, _other.Character };
            _sessionRegistry = new Mock<ISessionRegistry>();
            _sessionRegistry.Setup(s => s.TryGetCharacter(It.IsAny<Func<PlayerComponentBundle, bool>>(), out It.Ref<PlayerComponentBundle>.IsAny))
                .Returns(new TryGetCharacter((Func<PlayerComponentBundle, bool> predicate, out PlayerComponentBundle character) =>
                {
                    var found = characters.Where(predicate).ToList();
                    character = found.FirstOrDefault();
                    return found.Count > 0;
                }));
            _familyService = new Mock<IFamilyService>();
            _pubSubHub = new Mock<IPubSubHub>();
            _pubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>())).Callback<IMessage>(_published.Add).ReturnsAsync(true);
            _blacklistHub = new Mock<IBlacklistHub>();
            _blacklistHub.Setup(s => s.GetBlacklistedAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>());
            _head.Character.Family = Family((_head.Character.CharacterId, FamilyAuthority.Head));
        }

        private static GameObject.Services.FamilyService.Family Family(params (long CharacterId, FamilyAuthority Authority)[] members) => new()
        {
            FamilyId = FamilyId,
            Name = "Testers",
            MaxSize = 50,
            Members = members.Select(m => new FamilyCharacterDto { FamilyId = FamilyId, CharacterId = m.CharacterId, Authority = m.Authority }).ToList()
        };

        private Task InviteAsync(ClientSession inviter, string name) =>
            new FamilyInvitePacketHandler(_sessionRegistry.Object, _blacklistHub.Object)
                .ExecuteAsync(new FamilyInvitePacket { CharacterName = name }, inviter);

        private Task AnswerAsync(byte type) =>
            new JoinFamilyPacketHandler(_sessionRegistry.Object, _familyService.Object, _pubSubHub.Object, new Mock<IFamilyLogService>().Object)
                .ExecuteAsync(new JoinFamilyPacket { Type = type, CharacterId = _head.Character.CharacterId }, _other);

        private static Game18NConstString? LastInfo(ClientSession session) =>
            session.LastPackets.OfType<InfoiPacket>().LastOrDefault()?.Message
            ?? session.LastPackets.OfType<Infoi2Packet>().LastOrDefault()?.Message;

        [TestMethod]
        public async Task TheHeadInvitesAPlayerWithoutAFamily()
        {
            await InviteAsync(_head, _other.Character.Name);

            var question = _other.LastPackets.OfType<Dlgi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.AskMemberFamily, question.Question);
            Assert.AreEqual(1, ((JoinFamilyPacket)question.YesPacket!).Type);
            Assert.AreEqual(_head.Character.CharacterId, ((JoinFamilyPacket)question.YesPacket!).CharacterId);
            Assert.IsTrue(_head.Character.FamilyRequestCharacterIds.ContainsKey(_other.Character.CharacterId));
        }

        [TestMethod]
        public async Task APlayerWithoutAFamilyCannotInvite()
        {
            var third = await TestHelpers.Instance.GenerateSessionAsync();
            third.Character.Family = null;

            await InviteAsync(_other, third.Character.Name);

            Assert.AreEqual(0, third.LastPackets.OfType<Dlgi2Packet>().Count());
            Assert.IsEmpty(_other.Character.FamilyRequestCharacterIds);
        }

        [TestMethod]
        public async Task APlainMemberCannotInvite()
        {
            _other.Character.Family = Family((_head.Character.CharacterId, FamilyAuthority.Head), (_other.Character.CharacterId, FamilyAuthority.Member));

            await InviteAsync(_other, _head.Character.Name);

            Assert.AreEqual(Game18NConstString.NotAuthorizedToAcceptFamily, LastInfo(_other));
        }

        [TestMethod]
        public async Task AManagerInvitesOnlyWhenTheFamilyAllowsIt()
        {
            _head.Character.Family = Family((_head.Character.CharacterId, FamilyAuthority.Manager));

            await InviteAsync(_head, _other.Character.Name);

            Assert.AreEqual(Game18NConstString.NotAuthorizedToAcceptFamily, LastInfo(_head));
            Assert.AreEqual(0, _other.LastPackets.OfType<Dlgi2Packet>().Count());
        }

        [TestMethod]
        public async Task APlayerInAnotherFamilyCannotBeInvited()
        {
            _other.Character.Family = new GameObject.Services.FamilyService.Family { FamilyId = 7, Name = "Others" };

            await InviteAsync(_head, _other.Character.Name);

            Assert.AreEqual(Game18NConstString.PlayerAlreadyInAnotherFamily, LastInfo(_head));
        }

        [TestMethod]
        public async Task APlayerBlockingFamilyRequestsCannotBeInvited()
        {
            _other.Character.FamilyRequestBlocked = true;

            await InviteAsync(_head, _other.Character.Name);

            Assert.AreEqual(Game18NConstString.PlayerFamilyBlocked, LastInfo(_head));
        }

        [TestMethod]
        public async Task AFullFamilyCannotInvite()
        {
            _head.Character.Family!.MaxSize = 1;

            await InviteAsync(_head, _other.Character.Name);

            Assert.AreEqual(Game18NConstString.FamilyReachedMaxCapacity, LastInfo(_head));
        }

        [TestMethod]
        public async Task InvitingSomeoneOfflineSaysTheyCannotBeFound()
        {
            await InviteAsync(_head, "Nobody");

            Assert.AreEqual(Game18NConstString.CanNotFind, LastInfo(_head));
        }

        [TestMethod]
        public async Task AcceptingAnInvitationJoinsTheFamily()
        {
            await InviteAsync(_head, _other.Character.Name);

            await AnswerAsync(1);

            _familyService.Verify(s => s.AddMemberAsync(FamilyId, _otherId), Times.Once);
            var changed = (FamilyChangedData)_published.Single();
            CollectionAssert.AreEqual(new[] { _other.Character.CharacterId }, changed.CharacterIds);
            Assert.AreEqual(_other.Character.Name, changed.JoinedCharacterName);
        }

        [TestMethod]
        public async Task AnInvitationDoesNotFollowTheInviterIntoAnotherFamily()
        {
            await InviteAsync(_head, _other.Character.Name);
            var otherFamily = Family((_head.Character.CharacterId, FamilyAuthority.Head));
            otherFamily.FamilyId = FamilyId + 1;
            _head.Character.Family = otherFamily;

            await AnswerAsync(1);

            _familyService.Verify(s => s.AddMemberAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task AnInvitationLapsesWhenTheInviterLosesTheRightToInvite()
        {
            await InviteAsync(_head, _other.Character.Name);
            _head.Character.Family = Family((_head.Character.CharacterId, FamilyAuthority.Member));

            await AnswerAsync(1);

            _familyService.Verify(s => s.AddMemberAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task AnsweringWithoutAnInvitationDoesNothing()
        {
            await AnswerAsync(1);

            _familyService.Verify(s => s.AddMemberAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task DecliningTellsTheInviter()
        {
            await InviteAsync(_head, _other.Character.Name);
            _head.LastPackets.Clear();

            await AnswerAsync(2);

            Assert.AreEqual(Game18NConstString.PlayerRefusedFamilyInvite, LastInfo(_head));
            _familyService.Verify(s => s.AddMemberAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task TheHeadCannotLeave()
        {
            await new FamilyLeavePacketHandler(_familyService.Object, _pubSubHub.Object, new Mock<IFamilyLogService>().Object).ExecuteAsync(new FamilyLeavePacket(), _head);

            Assert.AreEqual(Game18NConstString.FamilyHeadCanNotLeave, LastInfo(_head));
            _familyService.Verify(s => s.RemoveMemberAsync(It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task AMemberLeaves()
        {
            _other.Character.Family = Family((_head.Character.CharacterId, FamilyAuthority.Head), (_other.Character.CharacterId, FamilyAuthority.Member));

            await new FamilyLeavePacketHandler(_familyService.Object, _pubSubHub.Object, new Mock<IFamilyLogService>().Object).ExecuteAsync(new FamilyLeavePacket(), _other);

            _familyService.Verify(s => s.RemoveMemberAsync(_otherId), Times.Once);
            Assert.AreEqual(Game18NConstString.LeftFamily, LastInfo(_other));
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        private Task DismissAsync(FamilyAuthority actor, FamilyAuthority target)
        {
            _head.Character.Family = Family((_head.Character.CharacterId, actor), (_other.Character.CharacterId, target));
            _familyService.Setup(s => s.FindCharacterAsync(_otherName))
                .ReturnsAsync(new CharacterDto { CharacterId = _other.Character.CharacterId, Name = _other.Character.Name });
            return new FamilyDismissPacketHandler(_familyService.Object, _pubSubHub.Object, new Mock<IFamilyLogService>().Object)
                .ExecuteAsync(new FamilyDismissPacket { CharacterName = _other.Character.Name }, _head);
        }

        [DataTestMethod]
        [DataRow(FamilyAuthority.Head, FamilyAuthority.Assistant)]
        [DataRow(FamilyAuthority.Head, FamilyAuthority.Member)]
        [DataRow(FamilyAuthority.Assistant, FamilyAuthority.Manager)]
        [DataRow(FamilyAuthority.Assistant, FamilyAuthority.Member)]
        public async Task HigherRanksDismissLowerOnes(FamilyAuthority actor, FamilyAuthority target)
        {
            await DismissAsync(actor, target);

            _familyService.Verify(s => s.RemoveMemberAsync(_otherId), Times.Once);
            CollectionAssert.AreEqual(new[] { _other.Character.CharacterId }, ((FamilyChangedData)_published.Single()).CharacterIds);
        }

        [DataTestMethod]
        [DataRow(FamilyAuthority.Assistant, FamilyAuthority.Head)]
        [DataRow(FamilyAuthority.Assistant, FamilyAuthority.Assistant)]
        public async Task NobodyDismissesTheirOwnRankOrAbove(FamilyAuthority actor, FamilyAuthority target)
        {
            await DismissAsync(actor, target);

            Assert.AreEqual(Game18NConstString.CanNotDismissedDeputy, LastInfo(_head));
            _familyService.Verify(s => s.RemoveMemberAsync(It.IsAny<long>()), Times.Never);
        }

        [DataTestMethod]
        [DataRow(FamilyAuthority.Manager)]
        [DataRow(FamilyAuthority.Member)]
        public async Task OnlyTheHeadAndAssistantsDismiss(FamilyAuthority actor)
        {
            await DismissAsync(actor, FamilyAuthority.Member);

            Assert.AreEqual(Game18NConstString.NotAuthorizedToAcceptFamily, LastInfo(_head));
            _familyService.Verify(s => s.RemoveMemberAsync(It.IsAny<long>()), Times.Never);
        }
    }
}

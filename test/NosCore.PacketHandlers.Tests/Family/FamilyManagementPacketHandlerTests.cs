//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using AutoFixture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.PacketHandlers.Family;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.AutoFixture;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyQuestObjective = NosCore.Data.Enumerations.Family.FamilyQuestObjective;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyManagementPacketHandlerTests
    {
        private TestWorld World;
        private const long FamilyId = 42;
        private ClientSession _head;
        private long _headId;
        private Mock<IFamilyService> _familyService;
        private Mock<IPubSubHub> _pubSubHub;
        private readonly List<IMessage> _published = new();
        private GameObject.Services.FamilyService.Family _family;
        private Mock<IFamilyHub> _familyHub;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.UseReal<IFamilyManagementService, FamilyManagementService>();
            World.UseReal<IFamilyContributionService, FamilyContributionService>();
            _familyHub = World.Freeze<Mock<IFamilyHub>>();
            _familyService = World.Freeze<Mock<IFamilyService>>();
            _pubSubHub = World.PubSubHub;
            _head = await World.CreateSessionAsync();
            _headId = _head.Character.CharacterId;
            _family = new GameObject.Services.FamilyService.Family
            {
                FamilyId = FamilyId,
                Name = "Testers",
                MaxSize = 50,
                Members = new List<FamilyCharacterDto>
                {
                    new() { FamilyId = FamilyId, CharacterId = _headId, Authority = FamilyAuthority.Head },
                    new() { FamilyId = FamilyId, CharacterId = 7, Authority = FamilyAuthority.Member, DailyMessage = "hi", Experience = 30 }
                }
            };
            _head.Character.Family = _family;
            _pubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>())).Callback<IMessage>(_published.Add).ReturnsAsync(true);
            _pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            _familyService.Setup(s => s.UpdateAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<FamilyChange?>(), It.IsAny<IReadOnlyList<FamilyMemberChange>>())).ReturnsAsync(true);
            _familyService.Setup(s => s.GetMemberCharactersAsync(It.IsAny<GameObject.Services.FamilyService.Family>())).ReturnsAsync(new List<CharacterDto>());
        }

        private void ChangeReturns(FamilyAuthorityChange result) =>
            _familyService.Setup(s => s.ChangeAuthorityAsync(_family, _headId, It.IsAny<long>(), It.IsAny<FamilyAuthority>()))
                .ReturnsAsync(result);

        private static Game18NConstString? LastInfo(ClientSession session) =>
            session.LastPackets.OfType<InfoiPacket>().LastOrDefault()?.Message
            ?? session.LastPackets.OfType<Infoi2Packet>().LastOrDefault()?.Message;

        [TestMethod]
        public async Task TheMemberListShowsEveryMemberAndWhoIsOnline()
        {
            _familyService.Setup(s => s.GetMemberCharactersAsync(_family)).ReturnsAsync(new List<CharacterDto>
            {
                new() { CharacterId = _headId, Name = "Head", Level = 90 },
                new() { CharacterId = 7, Name = "Buddy", Level = 12 }
            });
            _pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>
            {
                new() { ConnectedCharacter = new Character { Id = _headId } }
            });

            await World.Create<GListPacketHandler>()
                .ExecuteAsync(new GListPacket { Type = GListPacketType.MemberList }, _head);

            var members = _head.LastPackets.OfType<GmbrPacket>().Single().Members.NotNull();
            Assert.AreEqual(2, members.Count);
            Assert.IsTrue(members.Single(s => s.NotNull().CharacterId == _headId).NotNull().IsConnected);
            var buddy = members.Single(s => s.NotNull().CharacterId == 7).NotNull();
            Assert.AreEqual("Buddy", buddy.Name);
            Assert.AreEqual(Packets.Enumerations.FamilyAuthority.Member, buddy.Authority);
            Assert.IsFalse(buddy.IsConnected);
            Assert.AreEqual("hi", _head.LastPackets.OfType<GmsgPacket>().Single().Entries.NotNull().Single(s => s.NotNull().CharacterId == 7).NotNull().Message);
            Assert.AreEqual(30, _head.LastPackets.OfType<GexpPacket>().Single().Entries.NotNull().Single(s => s.NotNull().CharacterId == 7).NotNull().Experience);
        }

        [TestMethod]
        public async Task AppointingADeputyByNameRefreshesTheFamily()
        {
            _familyService.Setup(s => s.FindCharacterAsync("Buddy")).ReturnsAsync(new CharacterDto { CharacterId = 7 });
            ChangeReturns(FamilyAuthorityChange.Changed);

            await World.Create<FamilyDeputyPacketHandler>()
                .ExecuteAsync(new FamilyDeputyPacket { CharacterName = "Buddy" }, _head);

            _familyService.Verify(s => s.ChangeAuthorityAsync(_family, _headId, 7, FamilyAuthority.Assistant), Times.Once);
            Assert.AreEqual(Game18NConstString.NewFamilyDeputyAppointed, LastInfo(_head));
            CollectionAssert.AreEqual(new[] { 7L }, ((FamilyChangedData)_published.Single()).CharacterIds);
        }

        [TestMethod]
        public async Task PassingTheHeadRefreshesBothTags()
        {
            ChangeReturns(FamilyAuthorityChange.Changed);

            await World.Create<FamilyManagementPacketHandler>()
                .ExecuteAsync(new FamilyManagementPacket { TargetId = 7, FamilyAuthorityType = Packets.Enumerations.FamilyAuthority.Head }, _head);

            Assert.AreEqual(Game18NConstString.FamilyHeadChanged, LastInfo(_head));
            CollectionAssert.AreEquivalent(new[] { 7L, _headId }, ((FamilyChangedData)_published.Single()).CharacterIds);
        }

        [TestMethod]
        public async Task ARefusedChangeSaysWhyAndRefreshesNothing()
        {
            ChangeReturns(FamilyAuthorityChange.TooManyAssistants);

            await World.Create<FamilyManagementPacketHandler>()
                .ExecuteAsync(new FamilyManagementPacket { TargetId = 7, FamilyAuthorityType = Packets.Enumerations.FamilyAuthority.Assistant }, _head);

            Assert.AreEqual(Game18NConstString.CanNotAppointMoreDeputy, LastInfo(_head));
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task AnUnknownNameIsNotFound()
        {
            await World.Create<FamilyKeeperPacketHandler>()
                .ExecuteAsync(new FamilyKeeperPacket { CharacterName = "Nobody" }, _head);

            Assert.AreEqual(Game18NConstString.CanNotFind, LastInfo(_head));
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task TheHeadLetsKeepersInvite()
        {
            await World.Create<FAuthPacketHandler>()
                .ExecuteAsync(new FAuthPacket { MemberType = Packets.Enumerations.FamilyAuthority.Manager, AuthorityId = 0, Value = 1 }, _head);

            Assert.IsTrue(_family.ManagerCanInvite);
            _familyService.Verify(s => s.UpdateAsync(FamilyId, _headId, new FamilyChange { ManagerCanInvite = true },
                It.Is<IReadOnlyList<FamilyMemberChange>>(members => members.Count == 0)), Times.Once);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task ADailyMessageCountsTowardsTheFamilyAchievements()
        {
            _familyHub.Setup(s => s.AdvanceQuestsAsync(FamilyId, null, FamilyQuestObjective.DailyMessage, 1))
                .ReturnsAsync(new FamilyExperienceGain(1, false) { CompletedQuestIds = [9037] });

            await World.Create<FamilyTodayPacketHandler>()
                .ExecuteAsync(new FamilyTodayPacket { Message = "good morning" }, _head);

            CollectionAssert.AreEqual(new short[] { 9037 }, ((FamilyChangedData)_published.Single()).CompletedQuestIds);
        }

        [TestMethod]
        public async Task OnlyTheHeadChangesPermissions()
        {
            _family.Members = new List<FamilyCharacterDto>
            {
                new() { FamilyId = FamilyId, CharacterId = _headId, Authority = FamilyAuthority.Assistant }
            };

            await World.Create<FAuthPacketHandler>()
                .ExecuteAsync(new FAuthPacket { MemberType = Packets.Enumerations.FamilyAuthority.Manager, AuthorityId = 0, Value = 1 }, _head);

            Assert.IsFalse(_family.ManagerCanInvite);
            _familyService.Verify(s => s.UpdateAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<FamilyChange?>(), It.IsAny<IReadOnlyList<FamilyMemberChange>>()), Times.Never);
        }

        private void Become(FamilyAuthority authority) =>
            _family.Members.First(s => s.CharacterId == _headId).Authority = authority;

        [TestMethod]
        public async Task TheHeadSetsTheFamilyNotice()
        {
            await World.Create<FamilyNoticePacketHandler>()
                .ExecuteAsync(new FamilyNoticePacket { Message = "meet at the bank " }, _head);

            Assert.AreEqual("meet at the bank", _family.FamilyMessage);
            _familyService.Verify(s => s.UpdateAsync(FamilyId, _headId, new FamilyChange { FamilyMessage = "meet at the bank" },
                It.Is<IReadOnlyList<FamilyMemberChange>>(members => members.Count == 0)), Times.Once);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task AKeeperSetsTheNoticeOnlyWhenTheFamilyAllowsIt()
        {
            Become(FamilyAuthority.Manager);

            await World.Create<FamilyNoticePacketHandler>()
                .ExecuteAsync(new FamilyNoticePacket { Message = "hello" }, _head);

            Assert.AreEqual(Game18NConstString.NotAuthorizedToChangeNoticed, LastInfo(_head));
            Assert.IsNull(_family.FamilyMessage);

            _family.ManagerCanNotice = true;
            await World.Create<FamilyNoticePacketHandler>()
                .ExecuteAsync(new FamilyNoticePacket { Message = "hello" }, _head);

            Assert.AreEqual("hello", _family.FamilyMessage);
        }

        [TestMethod]
        public async Task AMemberCannotSetTheNotice()
        {
            Become(FamilyAuthority.Member);

            await World.Create<FamilyNoticePacketHandler>()
                .ExecuteAsync(new FamilyNoticePacket { Message = "hello" }, _head);

            Assert.AreEqual(Game18NConstString.NotAuthorizedToChangeNoticed, LastInfo(_head));
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task AnyMemberSetsTheirDailyMessage()
        {
            Become(FamilyAuthority.Member);

            await World.Create<FamilyTodayPacketHandler>()
                .ExecuteAsync(new FamilyTodayPacket { Message = "good morning" }, _head);

            var mine = _family.Members.First(s => s.CharacterId == _headId);
            Assert.AreEqual("good morning", mine.DailyMessage);
            _familyService.Verify(s => s.UpdateAsync(FamilyId, _headId, null, It.Is<IReadOnlyList<FamilyMemberChange>>(members =>
                members.Single() == new FamilyMemberChange(mine.CharacterId) { DailyMessage = "good morning" })), Times.Once);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task AMessageLongerThanItsColumnIsDropped()
        {
            var tooLong = new string('a', 256);

            await World.Create<FamilyNoticePacketHandler>()
                .ExecuteAsync(new FamilyNoticePacket { Message = tooLong }, _head);
            await World.Create<FamilyTodayPacketHandler>()
                .ExecuteAsync(new FamilyTodayPacket { Message = tooLong }, _head);

            Assert.IsNull(_family.FamilyMessage);
            Assert.AreNotEqual(tooLong, _family.Members.First(s => s.CharacterId == _headId).DailyMessage);
            _familyService.Verify(s => s.UpdateAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<FamilyChange?>(), It.IsAny<IReadOnlyList<FamilyMemberChange>>()), Times.Never);
        }

        [TestMethod]
        public async Task TheHeadGivesAMemberATitle()
        {
            _familyService.Setup(s => s.FindCharacterAsync("Buddy")).ReturnsAsync(new CharacterDto { CharacterId = 7, Name = "Buddy" });

            await World.Create<FamilyTitlePacketHandler>()
                .ExecuteAsync(new FamilyTitlePacket { CharacterName = "Buddy", Rank = Packets.Enumerations.FamilyMemberRank.Uncle }, _head);

            Assert.AreEqual(Data.Enumerations.Family.FamilyMemberRank.Uncle, _family.Members.First(s => s.CharacterId == 7).Rank);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task OnlyTheHeadGivesTitles()
        {
            Become(FamilyAuthority.Assistant);
            _familyService.Setup(s => s.FindCharacterAsync("Buddy")).ReturnsAsync(new CharacterDto { CharacterId = 7, Name = "Buddy" });

            await World.Create<FamilyTitlePacketHandler>()
                .ExecuteAsync(new FamilyTitlePacket { CharacterName = "Buddy", Rank = Packets.Enumerations.FamilyMemberRank.Uncle }, _head);

            Assert.AreEqual(Data.Enumerations.Family.FamilyMemberRank.Nothing, _family.Members.First(s => s.CharacterId == 7).Rank);
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task ATitleForSomeoneOutsideTheFamilyIsNotFound()
        {
            _familyService.Setup(s => s.FindCharacterAsync("Stranger")).ReturnsAsync(new CharacterDto { CharacterId = 99, Name = "Stranger" });

            await World.Create<FamilyTitlePacketHandler>()
                .ExecuteAsync(new FamilyTitlePacket { CharacterName = "Stranger", Rank = Packets.Enumerations.FamilyMemberRank.Uncle }, _head);

            Assert.AreEqual(Game18NConstString.CanNotFind, LastInfo(_head));
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task ChangingTheHeadGenderClearsEveryTitle()
        {
            _family.Members.First(s => s.CharacterId == 7).Rank = Data.Enumerations.Family.FamilyMemberRank.Uncle;

            await World.Create<FamilyGenderPacketHandler>()
                .ExecuteAsync(new FamilyGenderPacket { Gender = GenderType.Female }, _head);

            Assert.AreEqual(GenderType.Female, _family.FamilyHeadGender);
            Assert.IsTrue(_family.Members.All(s => s.Rank == Data.Enumerations.Family.FamilyMemberRank.Nothing));
            _familyService.Verify(s => s.UpdateAsync(FamilyId, _headId, new FamilyChange { FamilyHeadGender = GenderType.Female },
                It.Is<IReadOnlyList<FamilyMemberChange>>(members => members.Count == _family.Members.Count
                    && members.All(m => m.Rank == Data.Enumerations.Family.FamilyMemberRank.Nothing))), Times.Once);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task AGenderChangeTheMasterRefusesKeepsTheTitlesAndReloadsTheFamily()
        {
            _family.Members.First(s => s.CharacterId == 7).Rank = Data.Enumerations.Family.FamilyMemberRank.Uncle;
            _familyService.Setup(s => s.UpdateAsync(FamilyId, It.IsAny<long>(), It.IsAny<FamilyChange?>(), It.IsAny<IReadOnlyList<FamilyMemberChange>>()))
                .ReturnsAsync(false);

            await World.Create<FamilyGenderPacketHandler>()
                .ExecuteAsync(new FamilyGenderPacket { Gender = GenderType.Female }, _head);

            Assert.AreEqual(GenderType.Male, _family.FamilyHeadGender);
            Assert.AreEqual(Data.Enumerations.Family.FamilyMemberRank.Uncle, _family.Members.First(s => s.CharacterId == 7).Rank);
            var reload = (FamilyChangedData)_published.Single();
            Assert.AreEqual(FamilyId, reload.FamilyId);
            CollectionAssert.AreEqual(new[] { _headId }, reload.CharacterIds);
        }

        [TestMethod]
        public async Task OnlyTheHeadChangesTheHeadGender()
        {
            Become(FamilyAuthority.Assistant);

            await World.Create<FamilyGenderPacketHandler>()
                .ExecuteAsync(new FamilyGenderPacket { Gender = GenderType.Female }, _head);

            Assert.AreEqual(GenderType.Male, _family.FamilyHeadGender);
            Assert.AreEqual(0, _published.Count);
        }
    }
}

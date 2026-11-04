//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyManagementPacketHandlerTests
    {
        private const long FamilyId = 42;
        private ClientSession _head = null!;
        private long _headId;
        private Mock<IFamilyService> _familyService = null!;
        private Mock<IPubSubHub> _pubSubHub = null!;
        private readonly List<IMessage> _published = new();
        private GameObject.Services.FamilyService.Family _family = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _head = await TestHelpers.Instance.GenerateSessionAsync();
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
            _familyService = new Mock<IFamilyService>();
            _pubSubHub = new Mock<IPubSubHub>();
            _pubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>())).Callback<IMessage>(_published.Add).ReturnsAsync(true);
            _pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            _familyService.Setup(s => s.SaveAsync(It.IsAny<GameObject.Services.FamilyService.Family>())).ReturnsAsync(true);
            _familyService.Setup(s => s.SaveMembersAsync(It.IsAny<IEnumerable<FamilyCharacterDto>>())).ReturnsAsync(true);
            _familyService.Setup(s => s.GetMemberCharactersAsync(It.IsAny<GameObject.Services.FamilyService.Family>())).ReturnsAsync(new List<CharacterDto>());
        }

        private FamilyManagementService Management() => new(_familyService.Object, _pubSubHub.Object, new FamilyExperienceService(),
            new Mock<IDaoTransactionScope> { DefaultValue = DefaultValue.Mock }.Object);

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

            await new GListPacketHandler(Management())
                .ExecuteAsync(new GListPacket { Type = 2 }, _head);

            var members = _head.LastPackets.OfType<GmbrPacket>().Single().Members!;
            Assert.AreEqual(2, members.Count);
            Assert.IsTrue(members.Single(s => s!.CharacterId == _headId)!.IsConnected);
            var buddy = members.Single(s => s!.CharacterId == 7)!;
            Assert.AreEqual("Buddy", buddy.Name);
            Assert.AreEqual(Packets.Enumerations.FamilyAuthority.Member, buddy.Authority);
            Assert.IsFalse(buddy.IsConnected);
            Assert.AreEqual("hi", _head.LastPackets.OfType<GmsgPacket>().Single().Entries!.Single(s => s!.CharacterId == 7)!.Message);
            Assert.AreEqual(30, _head.LastPackets.OfType<GexpPacket>().Single().Entries!.Single(s => s!.CharacterId == 7)!.Experience);
        }

        [TestMethod]
        public async Task AppointingADeputyByNameRefreshesTheFamily()
        {
            _familyService.Setup(s => s.FindCharacterAsync("Buddy")).ReturnsAsync(new CharacterDto { CharacterId = 7 });
            ChangeReturns(FamilyAuthorityChange.Changed);

            await new FamilyDeputyPacketHandler(Management())
                .ExecuteAsync(new FamilyDeputyPacket { CharacterName = "Buddy" }, _head);

            _familyService.Verify(s => s.ChangeAuthorityAsync(_family, _headId, 7, FamilyAuthority.Assistant), Times.Once);
            Assert.AreEqual(Game18NConstString.NewFamilyDeputyAppointed, LastInfo(_head));
            CollectionAssert.AreEqual(new[] { 7L }, ((FamilyChangedData)_published.Single()).CharacterIds);
        }

        [TestMethod]
        public async Task PassingTheHeadRefreshesBothTags()
        {
            ChangeReturns(FamilyAuthorityChange.Changed);

            await new FamilyManagementPacketHandler(Management())
                .ExecuteAsync(new FamilyManagementPacket { TargetId = 7, FamilyAuthorityType = Packets.Enumerations.FamilyAuthority.Head }, _head);

            Assert.AreEqual(Game18NConstString.FamilyHeadChanged, LastInfo(_head));
            CollectionAssert.AreEquivalent(new[] { 7L, _headId }, ((FamilyChangedData)_published.Single()).CharacterIds);
        }

        [TestMethod]
        public async Task ARefusedChangeSaysWhyAndRefreshesNothing()
        {
            ChangeReturns(FamilyAuthorityChange.TooManyAssistants);

            await new FamilyManagementPacketHandler(Management())
                .ExecuteAsync(new FamilyManagementPacket { TargetId = 7, FamilyAuthorityType = Packets.Enumerations.FamilyAuthority.Assistant }, _head);

            Assert.AreEqual(Game18NConstString.CanNotAppointMoreDeputy, LastInfo(_head));
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task AnUnknownNameIsNotFound()
        {
            await new FamilyKeeperPacketHandler(Management())
                .ExecuteAsync(new FamilyKeeperPacket { CharacterName = "Nobody" }, _head);

            Assert.AreEqual(Game18NConstString.CanNotFind, LastInfo(_head));
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task TheHeadLetsKeepersInvite()
        {
            await new FAuthPacketHandler(_familyService.Object, _pubSubHub.Object)
                .ExecuteAsync(new FAuthPacket { MemberType = Packets.Enumerations.FamilyAuthority.Manager, AuthorityId = 0, Value = 1 }, _head);

            Assert.IsTrue(_family.ManagerCanInvite);
            _familyService.Verify(s => s.SaveAsync(_family), Times.Once);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task OnlyTheHeadChangesPermissions()
        {
            _family.Members = new List<FamilyCharacterDto>
            {
                new() { FamilyId = FamilyId, CharacterId = _headId, Authority = FamilyAuthority.Assistant }
            };

            await new FAuthPacketHandler(_familyService.Object, _pubSubHub.Object)
                .ExecuteAsync(new FAuthPacket { MemberType = Packets.Enumerations.FamilyAuthority.Manager, AuthorityId = 0, Value = 1 }, _head);

            Assert.IsFalse(_family.ManagerCanInvite);
            _familyService.Verify(s => s.SaveAsync(It.IsAny<GameObject.Services.FamilyService.Family>()), Times.Never);
        }

        private void Become(FamilyAuthority authority) =>
            _family.Members.First(s => s.CharacterId == _headId).Authority = authority;

        [TestMethod]
        public async Task TheHeadSetsTheFamilyNotice()
        {
            await new FamilyNoticePacketHandler(Management())
                .ExecuteAsync(new FamilyNoticePacket { Message = "meet at the bank " }, _head);

            Assert.AreEqual("meet at the bank", _family.FamilyMessage);
            _familyService.Verify(s => s.SaveAsync(_family), Times.Once);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task AKeeperSetsTheNoticeOnlyWhenTheFamilyAllowsIt()
        {
            Become(FamilyAuthority.Manager);

            await new FamilyNoticePacketHandler(Management())
                .ExecuteAsync(new FamilyNoticePacket { Message = "hello" }, _head);

            Assert.AreEqual(Game18NConstString.NotAuthorizedToChangeNoticed, LastInfo(_head));
            Assert.IsNull(_family.FamilyMessage);

            _family.ManagerCanNotice = true;
            await new FamilyNoticePacketHandler(Management())
                .ExecuteAsync(new FamilyNoticePacket { Message = "hello" }, _head);

            Assert.AreEqual("hello", _family.FamilyMessage);
        }

        [TestMethod]
        public async Task AMemberCannotSetTheNotice()
        {
            Become(FamilyAuthority.Member);

            await new FamilyNoticePacketHandler(Management())
                .ExecuteAsync(new FamilyNoticePacket { Message = "hello" }, _head);

            Assert.AreEqual(Game18NConstString.NotAuthorizedToChangeNoticed, LastInfo(_head));
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task AnyMemberSetsTheirDailyMessage()
        {
            Become(FamilyAuthority.Member);

            await new FamilyTodayPacketHandler(Management())
                .ExecuteAsync(new FamilyTodayPacket { Message = "good morning" }, _head);

            var mine = _family.Members.First(s => s.CharacterId == _headId);
            Assert.AreEqual("good morning", mine.DailyMessage);
            _familyService.Verify(s => s.SaveMembersAsync(It.Is<IEnumerable<FamilyCharacterDto>>(m => m.Single() == mine)), Times.Once);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task AMessageLongerThanItsColumnIsDropped()
        {
            var tooLong = new string('a', 256);

            await new FamilyNoticePacketHandler(Management())
                .ExecuteAsync(new FamilyNoticePacket { Message = tooLong }, _head);
            await new FamilyTodayPacketHandler(Management())
                .ExecuteAsync(new FamilyTodayPacket { Message = tooLong }, _head);

            Assert.IsNull(_family.FamilyMessage);
            Assert.AreNotEqual(tooLong, _family.Members.First(s => s.CharacterId == _headId).DailyMessage);
            _familyService.Verify(s => s.SaveAsync(It.IsAny<GameObject.Services.FamilyService.Family>()), Times.Never);
            _familyService.Verify(s => s.SaveMembersAsync(It.IsAny<IEnumerable<FamilyCharacterDto>>()), Times.Never);
        }

        [TestMethod]
        public async Task TheHeadGivesAMemberATitle()
        {
            _familyService.Setup(s => s.FindCharacterAsync("Buddy")).ReturnsAsync(new CharacterDto { CharacterId = 7, Name = "Buddy" });

            await new FamilyTitlePacketHandler(Management())
                .ExecuteAsync(new FamilyTitlePacket { CharacterName = "Buddy", Rank = Packets.Enumerations.FamilyMemberRank.Uncle }, _head);

            Assert.AreEqual(Data.Enumerations.Family.FamilyMemberRank.Uncle, _family.Members.First(s => s.CharacterId == 7).Rank);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task OnlyTheHeadGivesTitles()
        {
            Become(FamilyAuthority.Assistant);
            _familyService.Setup(s => s.FindCharacterAsync("Buddy")).ReturnsAsync(new CharacterDto { CharacterId = 7, Name = "Buddy" });

            await new FamilyTitlePacketHandler(Management())
                .ExecuteAsync(new FamilyTitlePacket { CharacterName = "Buddy", Rank = Packets.Enumerations.FamilyMemberRank.Uncle }, _head);

            Assert.AreEqual(Data.Enumerations.Family.FamilyMemberRank.Nothing, _family.Members.First(s => s.CharacterId == 7).Rank);
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task ATitleForSomeoneOutsideTheFamilyIsNotFound()
        {
            _familyService.Setup(s => s.FindCharacterAsync("Stranger")).ReturnsAsync(new CharacterDto { CharacterId = 99, Name = "Stranger" });

            await new FamilyTitlePacketHandler(Management())
                .ExecuteAsync(new FamilyTitlePacket { CharacterName = "Stranger", Rank = Packets.Enumerations.FamilyMemberRank.Uncle }, _head);

            Assert.AreEqual(Game18NConstString.CanNotFind, LastInfo(_head));
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task ChangingTheHeadGenderClearsEveryTitle()
        {
            _family.Members.First(s => s.CharacterId == 7).Rank = Data.Enumerations.Family.FamilyMemberRank.Uncle;

            await new FamilyGenderPacketHandler(Management())
                .ExecuteAsync(new FamilyGenderPacket { Gender = GenderType.Female }, _head);

            Assert.AreEqual(GenderType.Female, _family.FamilyHeadGender);
            Assert.IsTrue(_family.Members.All(s => s.Rank == Data.Enumerations.Family.FamilyMemberRank.Nothing));
            _familyService.Verify(s => s.SaveAsync(_family), Times.Once);
            Assert.AreEqual(FamilyId, ((FamilyChangedData)_published.Single()).FamilyId);
        }

        [TestMethod]
        public async Task AFailedGenderSaveKeepsTheTitles()
        {
            _family.Members.First(s => s.CharacterId == 7).Rank = Data.Enumerations.Family.FamilyMemberRank.Uncle;
            _familyService.Setup(s => s.SaveAsync(_family)).ReturnsAsync(false);

            await new FamilyGenderPacketHandler(Management())
                .ExecuteAsync(new FamilyGenderPacket { Gender = GenderType.Female }, _head);

            Assert.AreEqual(GenderType.Male, _family.FamilyHeadGender);
            Assert.AreEqual(Data.Enumerations.Family.FamilyMemberRank.Uncle, _family.Members.First(s => s.CharacterId == 7).Rank);
            Assert.AreEqual(0, _published.Count);
        }

        [TestMethod]
        public async Task OnlyTheHeadChangesTheHeadGender()
        {
            Become(FamilyAuthority.Assistant);

            await new FamilyGenderPacketHandler(Management())
                .ExecuteAsync(new FamilyGenderPacket { Gender = GenderType.Female }, _head);

            Assert.AreEqual(GenderType.Male, _family.FamilyHeadGender);
            Assert.AreEqual(0, _published.Count);
        }
    }
}

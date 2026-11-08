//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.PacketHandlers.Family;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Data.Dto;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyCreationPacketHandlerTests
    {
        private ClientSession _head = null!;
        private ClientSession _second = null!;
        private ClientSession _third = null!;
        private Mock<IFamilyService> _familyService = null!;
        private readonly Mock<IFamilyWarehouseService> _warehouse = new();
        private readonly List<IMessage> _published = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _head = await TestHelpers.Instance.GenerateSessionAsync();
            _second = await TestHelpers.Instance.GenerateSessionAsync();
            _third = await TestHelpers.Instance.GenerateSessionAsync();
            _head.Character.Gold = 200000;
            foreach (var session in new[] { _head, _second, _third })
            {
                session.Character.Class = CharacterClassType.Swordsman;
            }
            _familyService = new Mock<IFamilyService>();
            _familyService.Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<byte>(), It.IsAny<long>(), It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync(42);
        }

        private void FormGroup(params ClientSession[] sessions)
        {
            var group = new GameObject.Services.GroupService.Group(GroupType.Group, TestHelpers.Instance.SessionGroupFactory);
            foreach (var session in sessions)
            {
                session.Character.JoinGroup(group);
            }
        }

        private FamilyManagementService Management()
        {
            var pubSubHub = new Mock<IPubSubHub>();
            pubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>())).Callback<IMessage>(_published.Add).ReturnsAsync(true);
            return new FamilyManagementService(_familyService.Object, pubSubHub.Object, new FamilyExperienceService(),
                new Mock<IDaoTransactionScope> { DefaultValue = DefaultValue.Mock }.Object, TestHelpers.Instance.SessionRegistry,
                _warehouse.Object);
        }

        private static Game18NConstString? LastInfo(ClientSession session) =>
            session.LastPackets.OfType<InfoiPacket>().LastOrDefault()?.Message
            ?? session.LastPackets.OfType<Infoi2Packet>().LastOrDefault()?.Message;

        private Task CreateAsync(string name) =>
            new CreateFamilyPacketHandler(Management()).ExecuteAsync(new CreateFamilyPacket { FamilyName = name }, _head);

        [TestMethod]
        public async Task TheNpcAsksAFullGroupForAFamilyName()
        {
            FormGroup(_head, _second, _third);

            await new FamilyStartOrDisbandHandler(Management())
                .HandleAsync(_head, null, new NrunPacket { Runner = NrunRunnerType.FamilyStartOrDisband, Type = 0 });

            var inbox = _head.LastPackets.OfType<InboxPacket>().Single();
            Assert.IsInstanceOfType<CreateFamilyPacket>(inbox.BoxPacket);
            Assert.AreEqual(Game18NConstString.EnterFamilyName, inbox.Message);
        }

        [TestMethod]
        public async Task TheNpcTurnsAwayAGroupThatIsNotFull()
        {
            FormGroup(_head, _second);

            await new FamilyStartOrDisbandHandler(Management())
                .HandleAsync(_head, null, new NrunPacket { Runner = NrunRunnerType.FamilyStartOrDisband, Type = 0 });

            Assert.IsFalse(_head.LastPackets.OfType<InboxPacket>().Any());
            Assert.AreEqual(Game18NConstString.NeedThreeMember, LastInfo(_head));
        }

        [TestMethod]
        public async Task TheGroupFoundsTheFamilyWithTheAskerAsHead()
        {
            FormGroup(_head, _second, _third);

            await CreateAsync("Testers");

            var headId = _head.Character.CharacterId;
            var deputies = new HashSet<long> { _second.Character.CharacterId, _third.Character.CharacterId };
            _familyService.Verify(s => s.CreateAsync("Testers", 50, headId,
                It.Is<IEnumerable<long>>(ids => deputies.SetEquals(ids))), Times.Once);
            Assert.AreEqual(0, _head.Character.Gold);
            var created = _second.LastPackets.OfType<Msgi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.FamilyCreated, created.Message);
            CollectionAssert.AreEquivalent(
                new[] { _head.Character.CharacterId, _second.Character.CharacterId, _third.Character.CharacterId },
                ((FamilyChangedData)_published.Single()).CharacterIds);
        }

        [TestMethod]
        public async Task AGroupMemberInAFamilyBlocksTheCreation()
        {
            FormGroup(_head, _second, _third);
            _third.Character.Family = new GameObject.Services.FamilyService.Family { Name = "Others" };

            await CreateAsync("Testers");

            Assert.AreEqual(Game18NConstString.OneMemberAlreadyHaveAFamily, LastInfo(_head));
            _familyService.Verify(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<byte>(), It.IsAny<long>(), It.IsAny<IEnumerable<long>>()), Times.Never);
        }

        [TestMethod]
        public async Task AnAdventurerInTheGroupBlocksTheCreation()
        {
            FormGroup(_head, _second, _third);
            _second.Character.Class = CharacterClassType.Adventurer;

            await CreateAsync("Testers");

            Assert.AreEqual(Game18NConstString.OneOrMoreMemberHaveToChangeClass, LastInfo(_head));
            Assert.AreEqual(200000, _head.Character.Gold);
        }

        [TestMethod]
        public async Task CreatingCostsTwoHundredThousandGold()
        {
            FormGroup(_head, _second, _third);
            _head.Character.Gold = 199999;

            await CreateAsync("Testers");

            Assert.AreEqual(Game18NConstString.NeedMoneyForFamily, LastInfo(_head));
            Assert.AreEqual(199999, _head.Character.Gold);
        }

        [TestMethod]
        public async Task ATakenNameIsRefused()
        {
            FormGroup(_head, _second, _third);
            _familyService.Setup(s => s.IsNameTakenAsync("Testers")).ReturnsAsync(true);

            await CreateAsync("Testers");

            Assert.AreEqual(Game18NConstString.FamilyAlreadyExist, LastInfo(_head));
            Assert.AreEqual(200000, _head.Character.Gold);
        }

        [TestMethod]
        public async Task ANameOutsideThreeToFourteenCharactersIsRefused()
        {
            FormGroup(_head, _second, _third);

            await CreateAsync("Ab");
            Assert.AreEqual(Game18NConstString.FamilyNameTooShortOrTooLong, LastInfo(_head));

            await CreateAsync("FifteenLetters1");
            Assert.AreEqual(Game18NConstString.FamilyNameTooShortOrTooLong, LastInfo(_head));
            Assert.AreEqual(200000, _head.Character.Gold);
        }

        [DataTestMethod]
        [DataRow("Two Words")]
        [DataRow("Tab\tName")]
        public async Task ANameWithACharacterRefusedForCharacterNamesIsRefused(string name)
        {
            FormGroup(_head, _second, _third);

            await CreateAsync(name);

            Assert.AreEqual(Game18NConstString.InvalidCharacterInName, LastInfo(_head));
            Assert.AreEqual(200000, _head.Character.Gold);
            _familyService.Verify(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<byte>(), It.IsAny<long>(),
                It.IsAny<IEnumerable<long>>()), Times.Never);
        }

        private void JoinFamily(FamilyAuthority authority)
        {
            _head.Character.Family = new GameObject.Services.FamilyService.Family
            {
                FamilyId = 7,
                Name = "Testers",
                Members = new List<FamilyCharacterDto>
                {
                    new() { FamilyId = 7, CharacterId = _head.Character.CharacterId, Authority = authority },
                    new() { FamilyId = 7, CharacterId = _second.Character.CharacterId, Authority = FamilyAuthority.Member }
                }
            };
        }

        [TestMethod]
        public async Task TheNpcAsksTheHeadToConfirmTheDisband()
        {
            JoinFamily(FamilyAuthority.Head);

            await new FamilyStartOrDisbandHandler(Management())
                .HandleAsync(_head, null, new NrunPacket { Runner = NrunRunnerType.FamilyStartOrDisband, Type = 1 });

            var question = _head.LastPackets.OfType<QnaiPacket>().Single();
            Assert.IsInstanceOfType<FamilyDismissPacket>(question.YesPacket);
            Assert.AreEqual(Game18NConstString.AreYouSureToDisbandFamily, question.Question);
        }

        [TestMethod]
        public async Task OnlyTheHeadCanDisband()
        {
            JoinFamily(FamilyAuthority.Assistant);

            await new FamilyDisbandPacketHandler(Management()).ExecuteAsync(new FamilyDismissPacket(), _head);

            Assert.AreEqual(Game18NConstString.OnlyHeadCanDisbandFamily, LastInfo(_head));
            _familyService.Verify(s => s.DisbandAsync(It.IsAny<GameObject.Services.FamilyService.Family>()), Times.Never);
        }

        [TestMethod]
        public async Task DisbandingRemovesEveryMember()
        {
            JoinFamily(FamilyAuthority.Head);
            var family = _head.Character.Family!;
            _familyService.Setup(s => s.DisbandAsync(family)).ReturnsAsync(true);

            await new FamilyDisbandPacketHandler(Management()).ExecuteAsync(new FamilyDismissPacket(), _head);

            Assert.AreEqual(Game18NConstString.FamilyDisbanded, LastInfo(_head));
            _warehouse.Verify(s => s.EmptyAsync(7), Times.Once);
            CollectionAssert.AreEquivalent(new[] { _head.Character.CharacterId, _second.Character.CharacterId },
                ((FamilyChangedData)_published.Single()).CharacterIds);
        }

        [TestMethod]
        public async Task AFailedDisbandChangesNothing()
        {
            JoinFamily(FamilyAuthority.Head);

            await new FamilyDisbandPacketHandler(Management()).ExecuteAsync(new FamilyDismissPacket(), _head);

            Assert.AreEqual(Game18NConstString.FailedToDisbandFamily, LastInfo(_head));
            Assert.IsFalse(_published.Any());
        }
    }
}

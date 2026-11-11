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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Marriage
{
    [TestClass]
    public class MarriageHandlerTests
    {
        private const short ArrowVNum = 1981;
        private const short LetterVNum = 1982;

        private TestWorld World;
        private ClientSession _proposer;
        private ClientSession _beloved;
        private Mock<IFriendHub> _friendHub;
        private Mock<ISessionRegistry> _sessionRegistry;
        private long _proposerId;
        private long _belovedId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _proposer = await World.CreateSessionAsync();
            _beloved = await World.CreateSessionAsync();
            _proposerId = _proposer.Character.CharacterId;
            _belovedId = _beloved.Character.CharacterId;
            _friendHub = World.Freeze<Mock<IFriendHub>>();
            _friendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>());
            World.Freeze<Mock<IPubSubHub>>().Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            _sessionRegistry = World.Freeze<Mock<ISessionRegistry>>();
            _sessionRegistry.Setup(s => s.GetSessionByCharacterId(_proposerId)).Returns(_proposer);
            _sessionRegistry.Setup(s => s.GetSessionByCharacterId(_belovedId)).Returns(_beloved);
            _sessionRegistry.Setup(s => s.GetSessions(It.IsAny<Func<ClientSession, bool>?>())).Returns([_proposer, _beloved]);
        }

        [TestMethod]
        public async Task TheArrowAsksBeforeProposing()
        {
            Befriend();
            var arrow = Give(_proposer, ArrowVNum, ItemEffectType.MarriageProposal);

            await ProposeAsync(arrow, 0);

            var question = _proposer.LastPackets.OfType<Qnai2Packet>().Single();
            Assert.AreEqual(Game18NConstString.AskPropose, question.Question);
            Assert.AreEqual(1, _proposer.Character.InventoryService.CountItem(ArrowVNum));
            Assert.IsFalse(_beloved.LastPackets.OfType<Dlgi2Packet>().Any());
        }

        [TestMethod]
        public async Task AConfirmedProposalUsesTheArrowAndAsksTheBeloved()
        {
            Befriend();
            var arrow = Give(_proposer, ArrowVNum, ItemEffectType.MarriageProposal);

            await ProposeAsync(arrow, 2);

            Assert.AreEqual(0, _proposer.Character.InventoryService.CountItem(ArrowVNum));
            var dialog = _beloved.LastPackets.OfType<Dlgi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.AskAcceptPropose, dialog.Question);
            var yes = (ClientGuriPacket)dialog.YesPacket.NotNull();
            Assert.AreEqual(GuriPacketType.Wedding, yes.Type);
            Assert.AreEqual(1, yes.Argument);
            Assert.AreEqual(_proposerId, yes.VisualId);
        }

        [TestMethod]
        public async Task OnlyAFriendCanBeProposedTo()
        {
            var arrow = Give(_proposer, ArrowVNum, ItemEffectType.MarriageProposal);

            await ProposeAsync(arrow, 2);

            Assert.AreEqual(1, _proposer.Character.InventoryService.CountItem(ArrowVNum));
            Assert.IsTrue(_proposer.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == Game18NConstString.SelectAppropriateTarget));
        }

        [TestMethod]
        public async Task AMarriedPlayerCannotPropose()
        {
            _friendHub.Setup(s => s.GetFriendsAsync(_proposerId)).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new() { CharacterId = _belovedId, RelationType = CharacterRelationType.Friend },
                new() { CharacterId = 99, RelationType = CharacterRelationType.Spouse }
            });
            var arrow = Give(_proposer, ArrowVNum, ItemEffectType.MarriageProposal);

            await ProposeAsync(arrow, 2);

            Assert.AreEqual(1, _proposer.Character.InventoryService.CountItem(ArrowVNum));
            Assert.IsTrue(_proposer.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == Game18NConstString.AlreadyMarried));
        }

        [TestMethod]
        public async Task AcceptingTheProposalMarriesTheCouple()
        {
            Befriend();
            _friendHub.Setup(s => s.MarryAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(true);
            await ProposeAsync(Give(_proposer, ArrowVNum, ItemEffectType.MarriageProposal), 2);

            await AnswerAsync(1);

            _friendHub.Verify(s => s.MarryAsync(_proposerId, _belovedId), Times.Once);
            Assert.IsTrue(_proposer.LastPackets.OfType<Msgi2Packet>().Any(p => p.Message == Game18NConstString.PlayersMarried));
        }

        [TestMethod]
        public async Task DecliningTheProposalMarriesNobody()
        {
            Befriend();
            await ProposeAsync(Give(_proposer, ArrowVNum, ItemEffectType.MarriageProposal), 2);

            await AnswerAsync(0);

            _friendHub.Verify(s => s.MarryAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
            Assert.IsTrue(_proposer.LastPackets.OfType<Msgi2Packet>().Any(p => p.Message == Game18NConstString.ProposalDeclined));
        }

        [TestMethod]
        public async Task AnAnswerWithoutAProposalIsIgnored()
        {
            await AnswerAsync(1);

            _friendHub.Verify(s => s.MarryAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task AProposalIsAnsweredOnlyOnce()
        {
            Befriend();
            _friendHub.Setup(s => s.MarryAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(true);
            await ProposeAsync(Give(_proposer, ArrowVNum, ItemEffectType.MarriageProposal), 2);

            await AnswerAsync(1);
            await AnswerAsync(1);

            _friendHub.Verify(s => s.MarryAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public async Task TheLetterAsksBeforeSeparating()
        {
            Marry();
            var letter = Give(_proposer, LetterVNum, ItemEffectType.MarriageSeparation);

            await SeparateAsync(letter, 0);

            Assert.AreEqual(Game18NConstString.AskSeparateSpouse, _proposer.LastPackets.OfType<QnaiPacket>().Single().Question);
            _friendHub.Verify(s => s.SeparateAsync(It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task TheLetterSeparatesTheCouple()
        {
            Marry();
            _friendHub.Setup(s => s.SeparateAsync(_proposerId)).ReturnsAsync(_belovedId);
            var letter = Give(_proposer, LetterVNum, ItemEffectType.MarriageSeparation);

            await SeparateAsync(letter, 1);

            Assert.AreEqual(0, _proposer.Character.InventoryService.CountItem(LetterVNum));
            Assert.IsTrue(_proposer.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == Game18NConstString.SpouseSeparated));
        }

        [TestMethod]
        public async Task TheLetterIsKeptWithoutASpouse()
        {
            var letter = Give(_proposer, LetterVNum, ItemEffectType.MarriageSeparation);

            await SeparateAsync(letter, 1);

            Assert.AreEqual(1, _proposer.Character.InventoryService.CountItem(LetterVNum));
            _friendHub.Verify(s => s.SeparateAsync(It.IsAny<long>()), Times.Never);
        }

        private void Befriend()
        {
            _friendHub.Setup(s => s.GetFriendsAsync(_proposerId)).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new() { CharacterId = _belovedId, RelationType = CharacterRelationType.Friend }
            });
            _friendHub.Setup(s => s.GetFriendsAsync(_belovedId)).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new() { CharacterId = _proposerId, RelationType = CharacterRelationType.Friend }
            });
        }

        private void Marry() =>
            _friendHub.Setup(s => s.GetFriendsAsync(_proposerId)).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new() { CharacterId = _belovedId, RelationType = CharacterRelationType.Spouse }
            });

        private Task ProposeAsync(InventoryItemInstance arrow, byte mode) =>
            World.Create<MarriageProposalHandler>().Handle(new ItemUsedEvent(_proposer, arrow, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _belovedId,
                Type = PocketType.Main,
                Slot = arrow.Slot,
                Mode = mode
            }));

        private Task SeparateAsync(InventoryItemInstance letter, byte mode) =>
            World.Create<SeparationLetterHandler>().Handle(new ItemUsedEvent(_proposer, letter, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _proposerId,
                Type = PocketType.Main,
                Slot = letter.Slot,
                Mode = mode
            }));

        private Task AnswerAsync(int argument) =>
            World.Create<WeddingHandler>().Handle(new GuriPacketReceivedEvent(_beloved, new ClientGuriPacket
            {
                Type = GuriPacketType.Wedding,
                Argument = argument,
                VisualId = _proposerId
            }));

        private static InventoryItemInstance Give(ClientSession session, short vnum, ItemEffectType effect)
        {
            var item = new Item { VNum = vnum, Type = NoscorePocketType.Main, Effect = effect };
            var instance = InventoryItemInstance.Create(new ItemInstanceForTest(vnum) { Amount = 1, Item = item },
                session.Character.CharacterId);
            instance.Slot = 0;
            instance.Type = NoscorePocketType.Main;
            session.Character.InventoryService[instance.ItemInstanceId] = instance;
            return instance;
        }

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}

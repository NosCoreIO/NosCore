//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using Microsoft.Extensions.Logging.Abstractions;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MailService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Tests.Services.MailService
{
    [TestClass]
    public class GiftMailTests
    {
        private TestWorld World;
        private const long SenderId = 1;
        private const long ReceiverId = 2;
        private const long WorldChannelId = 7;
        private const short Seed = 1012;
        private const short Sword = 1;
        private const short Specialist = 912;

        private readonly List<ItemDto> _items = new()
        {
            new Item { VNum = Seed, Type = NoscorePocketType.Main, ItemType = ItemType.Main },
            new Item { VNum = Sword, Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon },
            new Item { VNum = Specialist, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist }
        };

        private readonly List<IItemInstanceDto> _createdItems = new();
        private readonly List<MailDto> _savedMails = new();
        private Mock<IParcelRegistry> _parcelRegistry;
        private Mock<IPubSubHub> _pubSubHub;
        private IMailService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();

            var characters = new Mock<IDao<CharacterDto, long>>();
            characters.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns<Expression<Func<CharacterDto, bool>>>(predicate => Task.FromResult<CharacterDto?>(new[]
                {
                    new CharacterDto { CharacterId = SenderId, Name = "Sender" },
                    new CharacterDto { CharacterId = ReceiverId, Name = "Receiver" }
                }.AsQueryable().FirstOrDefault(predicate)));

            var itemInstances = new Mock<IDao<IItemInstanceDto?, Guid>>();
            itemInstances.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                .Returns<IItemInstanceDto?>(item =>
                {
                    _createdItems.Add(item.NotNull());
                    return Task.FromResult(item);
                });

            var mails = new Mock<IDao<MailDto, long>>();
            mails.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<MailDto>()))
                .Returns<MailDto>(mail =>
                {
                    _savedMails.Add(mail);
                    return Task.FromResult(mail);
                });

            var channels = new Mock<IChannelHub>();
            channels.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo>
            {
                new() { Host = "127.0.0.1", Id = WorldChannelId, Type = ServerType.WorldServer }
            });

            _pubSubHub = new Mock<IPubSubHub>();
            _pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            _parcelRegistry = new Mock<IParcelRegistry>();
            _parcelRegistry.Setup(s => s.TryAddMail(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<MailData>())).Returns(true);

            _service = new GameObject.Services.MailService.MailService(mails.Object, itemInstances.Object, _pubSubHub.Object,
                channels.Object, _items, new ItemGenerationService(_items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService()),
                _parcelRegistry.Object, characters.Object);
        }

        private static MailDto Gift(long? senderId = SenderId, long receiverId = ReceiverId) =>
            new() { SenderId = senderId, ReceiverId = receiverId, Title = "gift" };

        [TestMethod]
        public async Task AGiftToAnUnknownCharacterIsNotSent()
        {
            Assert.IsFalse(await _service.SendMailAsync(Gift(receiverId: 99), Seed, 1, 0, 0));
            Assert.AreEqual(0, _savedMails.Count);
        }

        [TestMethod]
        public async Task AGiftOfAnUnknownItemIsNotSent()
        {
            Assert.IsFalse(await _service.SendMailAsync(Gift(), 9999, 1, 0, 0));
            Assert.AreEqual(0, _createdItems.Count);
            Assert.AreEqual(0, _savedMails.Count);
        }

        [DataTestMethod]
        [DataRow(Seed, (short)5, (sbyte)3, (byte)4, (short)5, (sbyte)0, (byte)0, DisplayName = "A stackable keeps its amount and loses rarity and upgrade")]
        [DataRow(Seed, (short)0, (sbyte)0, (byte)0, (short)1, (sbyte)0, (byte)0, DisplayName = "An amount of zero becomes one")]
        [DataRow(Sword, (short)5, (sbyte)7, (byte)10, (short)1, (sbyte)7, (byte)10, DisplayName = "A weapon is a single item with its rarity and upgrade")]
        [DataRow(Sword, (short)1, (sbyte)9, (byte)11, (short)1, (sbyte)0, (byte)0, DisplayName = "A weapon's rarity above 8 and upgrade above 10 are dropped")]
        [DataRow(Sword, (short)1, (sbyte)-3, (byte)0, (short)1, (sbyte)0, (byte)0, DisplayName = "A weapon's rarity below -2 is dropped")]
        [DataRow(Specialist, (short)1, (sbyte)5, (byte)15, (short)1, (sbyte)0, (byte)15, DisplayName = "A specialist keeps an upgrade up to 15 and no rarity")]
        [DataRow(Specialist, (short)1, (sbyte)0, (byte)16, (short)1, (sbyte)0, (byte)0, DisplayName = "A specialist's upgrade above 15 is dropped")]
        public async Task TheGiftedItemIsClampedToWhatItsTypeAllows(short vnum, short amount, sbyte rare, byte upgrade,
            short expectedAmount, sbyte expectedRare, byte expectedUpgrade)
        {
            Assert.IsTrue(await _service.SendMailAsync(Gift(), vnum, amount, rare, upgrade));

            var item = _createdItems[0];
            Assert.AreEqual(vnum, item.ItemVNum);
            Assert.AreEqual(expectedAmount, item.Amount);
            Assert.AreEqual(expectedRare, item.Rare);
            Assert.AreEqual(expectedUpgrade, item.Upgrade);
            Assert.AreEqual(item.Id, _savedMails[0].ItemInstanceId);
        }

        [TestMethod]
        public async Task APlayerGiftAlsoKeepsASenderCopy()
        {
            await _service.SendMailAsync(Gift(), Seed, 1, 0, 0);

            Assert.AreEqual(2, _savedMails.Count);
            Assert.IsFalse(_savedMails[0].IsSenderCopy);
            Assert.IsTrue(_savedMails[1].IsSenderCopy);
            Assert.AreNotEqual(_savedMails[0].ItemInstanceId, _savedMails[1].ItemInstanceId);
            _parcelRegistry.Verify(s => s.TryAddMail(ReceiverId, It.IsAny<bool>(), It.IsAny<MailData>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task ANosMallGiftHasNoSenderCopy()
        {
            await _service.SendMailAsync(Gift(senderId: null), Seed, 1, 0, 0);

            Assert.AreEqual(1, _savedMails.Count);
            _parcelRegistry.Verify(s => s.TryAddMail(ReceiverId, false,
                It.Is<MailData>(m => m.SenderName == "NOSMALL" && m.ReceiverName == "Receiver")), Times.Once);
        }

        [TestMethod]
        public async Task AnOnlineReceiverIsNotified()
        {
            _pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>
            {
                new() { ChannelId = WorldChannelId, Name = "Receiver", ConnectedCharacter = new Character { Id = ReceiverId, Name = "Receiver" } }
            });

            await _service.SendMailAsync(Gift(senderId: null), Seed, 1, 0, 0);

            _pubSubHub.Verify(s => s.SendMessageAsync(It.Is<MailData>(m => m.ReceiverName == "Receiver")), Times.Once);
        }

        [TestMethod]
        public async Task AnOfflineReceiverIsNotNotified()
        {
            await _service.SendMailAsync(Gift(senderId: null), Seed, 1, 0, 0);

            _pubSubHub.Verify(s => s.SendMessageAsync(It.IsAny<MailData>()), Times.Never);
        }
    }
}

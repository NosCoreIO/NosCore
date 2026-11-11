//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MailService;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.Tests.Services.MailService
{
    [TestClass]
    public class ParcelRegistryTests
    {
        private TestWorld World;
        private const long SenderId = 1;
        private const long ReceiverId = 2;
        private const short Seed = 1012;
        private const int MailCount = 10;

        private readonly List<ItemDto> _items = new()
        {
            new Item { VNum = Seed, Type = NoscorePocketType.Main, ItemType = ItemType.Main }
        };

        private readonly List<MailDto> _storedMails = new();
        private Mock<IDao<CharacterDto, long>> _characters;
        private Mock<IDao<MailDto, long>> _mails;
        private Mock<IDao<IItemInstanceDto?, Guid>> _itemInstances;
        private ParcelRegistry _registry;
        private long _lastMailId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();

            var characters = new[]
            {
                new CharacterDto { CharacterId = SenderId, Name = "Sender" },
                new CharacterDto { CharacterId = ReceiverId, Name = "Receiver" }
            };
            _characters = new Mock<IDao<CharacterDto, long>>();
            _characters.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns<Expression<Func<CharacterDto, bool>>>(async predicate =>
                {
                    await Task.Delay(20);
                    return characters.AsQueryable().FirstOrDefault(predicate);
                });
            _characters.Setup(s => s.Where(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns<Expression<Func<CharacterDto, bool>>>(predicate => characters.AsQueryable().Where(predicate).ToList());

            _itemInstances = new Mock<IDao<IItemInstanceDto?, Guid>>();
            _itemInstances.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                .Returns<IItemInstanceDto?>(Task.FromResult);
            _itemInstances.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .Returns(async () =>
                {
                    await Task.Delay(20);
                    return null;
                });
            _itemInstances.Setup(s => s.Where(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .Returns(new List<IItemInstanceDto?>());

            _mails = new Mock<IDao<MailDto, long>>();
            _mails.Setup(s => s.LoadAll()).Returns(() => _storedMails);
            _mails.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<MailDto>()))
                .Returns<MailDto>(mail =>
                {
                    mail.MailId = Interlocked.Increment(ref _lastMailId);
                    return Task.FromResult(mail);
                });
        }

        [TestMethod]
        public async Task MailsSentTogetherToOneCharacterAreAllKept()
        {
            await new Spec("Mails sent together to one character are all kept")
                .Given(TheRegistryIsStarted)
                .WhenAsync(SeveralGiftsAreSentAtOnce)
                .Then(TheReceiverShouldHaveEveryGift)
                .And(EveryGiftShouldHaveItsOwnId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StoredMailsAreAvailableAsSoonAsTheRegistryExists()
        {
            await new Spec("Stored mails are available as soon as the registry exists")
                .Given(AMailIsStored)
                .When(TheRegistryIsStarted)
                .Then(TheStoredMailShouldBeListed)
                .ExecuteAsync();
        }

        private void AMailIsStored()
        {
            _storedMails.Add(new MailDto { MailId = 1, SenderId = SenderId, ReceiverId = ReceiverId, Title = "note" });
        }

        private void TheRegistryIsStarted()
        {
            _registry = new ParcelRegistry(_characters.Object, _mails.Object, _items, _itemInstances.Object);
        }

        private async Task SeveralGiftsAreSentAtOnce()
        {
            var channels = new Mock<IChannelHub>();
            channels.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo>());
            var pubSubHub = new Mock<IPubSubHub>();
            pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            var service = new GameObject.Services.MailService.MailService(_mails.Object, _itemInstances.Object,
                pubSubHub.Object, channels.Object, _items,
                new ItemGenerationService(_items, NullLoggerFactory.Instance, World.LogLanguageLocalizer),
                _registry, _characters.Object);

            var results = await Task.WhenAll(Enumerable.Range(0, MailCount).Select(_ => Task.Run(() =>
                service.SendMailAsync(new MailDto { SenderId = SenderId, ReceiverId = ReceiverId, Title = "gift" },
                    Seed, 1, 0, 0))));
            Assert.IsTrue(results.All(sent => sent));
        }

        private void TheReceiverShouldHaveEveryGift()
        {
            Assert.AreEqual(MailCount, _registry.GetMails(ReceiverId, false).Count);
        }

        private void EveryGiftShouldHaveItsOwnId()
        {
            var mails = _registry.GetMails(ReceiverId, false);
            Assert.IsTrue(mails.All(s => s.Key == s.Value.MailId));
            Assert.AreEqual(MailCount, mails.Values.Select(s => s.MailDto.MailId).Distinct().Count());
        }

        private void TheStoredMailShouldBeListed()
        {
            var mail = _registry.GetMail(ReceiverId, false, 0);
            Assert.IsNotNull(mail);
            Assert.AreEqual("Sender", mail.SenderName);
            Assert.AreEqual("Receiver", mail.ReceiverName);
        }
    }
}

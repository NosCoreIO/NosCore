//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MailService;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Tests.Services.MailService
{
    [TestClass]
    public class MailServiceTests
    {
        private IMailService Service = null!;
        private Mock<IDao<MailDto, long>> MailDao = null!;
        private Mock<IDao<IItemInstanceDto?, Guid>> ItemInstanceDao = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private Mock<IItemGenerationService> ItemProvider = null!;
        private Mock<IParcelRegistry> ParcelRegistry = null!;
        private Mock<IDao<CharacterDto, long>> CharacterDao = null!;
        private List<ItemDto> Items = null!;
        private long CharacterId = 1;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();

            MailDao = new Mock<IDao<MailDto, long>>();
            ItemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            PubSubHub = new Mock<IPubSubHub>();
            ChannelHub = new Mock<IChannelHub>();
            ItemProvider = new Mock<IItemGenerationService>();
            ParcelRegistry = new Mock<IParcelRegistry>();
            CharacterDao = new Mock<IDao<CharacterDto, long>>();
            Items = new List<ItemDto>();

            Service = new GameObject.Services.MailService.MailService(
                MailDao.Object,
                ItemInstanceDao.Object,
                PubSubHub.Object,
                ChannelHub.Object,
                Items,
                ItemProvider.Object,
                ParcelRegistry.Object,
                CharacterDao.Object);
        }

        [TestMethod]
        public async Task GetMailsShouldReturnEmptyListWhenNoMails()
        {
            await new Spec("Get mails should return empty list when no mails")
                .Given(NoMailsExist)
                .When(GettingAllMails)
                .Then(ResultShouldBeEmptyList)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetMailsShouldReturnMailsWhenMailsExist()
        {
            await new Spec("Get mails should return mails when mails exist")
                .Given(MailsExist)
                .When(GettingAllMails)
                .Then(ResultShouldContainMails)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetMailsShouldReturnSpecificMailWhenIdProvided()
        {
            await new Spec("Get mails should return specific mail when ID provided")
                .Given(MailsExist)
                .And(SpecificMailExists)
                .When(GettingSpecificMail)
                .Then(ResultShouldContainOneMail)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ServiceCanBeConstructed()
        {
            await new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .ExecuteAsync();
        }

        private List<MailData>? MailResult;

        private void NoMailsExist()
        {
            ParcelRegistry.Setup(s => s.GetMails(CharacterId, false))
                .Returns(new ConcurrentDictionary<long, MailData>());
            ParcelRegistry.Setup(s => s.GetMails(CharacterId, true))
                .Returns(new ConcurrentDictionary<long, MailData>());
        }

        private void MailsExist()
        {
            var mails = new ConcurrentDictionary<long, MailData>();
            mails.TryAdd(1, new MailData { MailDto = new MailDto { MailId = 1 } });
            mails.TryAdd(2, new MailData { MailDto = new MailDto { MailId = 2 } });
            ParcelRegistry.Setup(s => s.GetMails(CharacterId, false))
                .Returns(mails);
            ParcelRegistry.Setup(s => s.GetMails(CharacterId, true))
                .Returns(new ConcurrentDictionary<long, MailData>());
        }

        private void SpecificMailExists()
        {
            ParcelRegistry.Setup(s => s.GetMail(CharacterId, false, 1))
                .Returns(new MailData { MailDto = new MailDto { MailId = 1 } });
        }

        private void GettingAllMails()
        {
            MailResult = Service.GetMails(-1, CharacterId, false);
        }

        private void GettingSpecificMail()
        {
            MailResult = Service.GetMails(1, CharacterId, false);
        }

        private void ResultShouldBeEmptyList()
        {
            Assert.IsNotNull(MailResult);
            Assert.AreEqual(0, MailResult.Count);
        }

        private void ResultShouldContainMails()
        {
            Assert.IsNotNull(MailResult);
            Assert.AreEqual(2, MailResult.Count);
        }

        private void ResultShouldContainOneMail()
        {
            Assert.IsNotNull(MailResult);
            Assert.AreEqual(1, MailResult.Count);
        }

        private void ServiceShouldNotBeNull()
        {
            Assert.IsNotNull(Service);
        }
    }
}

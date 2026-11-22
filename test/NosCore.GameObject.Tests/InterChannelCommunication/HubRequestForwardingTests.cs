//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Services.BazaarService;
using NosCore.GameObject.Services.BlackListService;
using NosCore.GameObject.Services.FriendService;
using NosCore.GameObject.Services.MailService;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using SpecLight;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class HubRequestForwardingTests
    {
        private const long CharacterId = 1;
        private const long TargetId = 2;
        private readonly Guid OperationId = Guid.NewGuid();
        private readonly ItemInstanceDto Item = new() { Id = Guid.NewGuid(), ItemVNum = 1012, Amount = 3 };
        private readonly MailDto Mail = new() { MailId = 1, SenderId = CharacterId, ReceiverId = TargetId, Title = "note" };
        private readonly Mock<IFriendService> FriendService = new();
        private readonly Mock<IBlacklistService> BlacklistService = new();
        private readonly Mock<IWarehouseService> WarehouseService = new();
        private readonly Mock<IMailService> MailService = new();
        private readonly Mock<IBazaarService> BazaarService = new();
        private object? Result;

        [TestMethod]
        public async Task FriendRequestReachesTheFriendService()
        {
            await new Spec("Friend request reaches the friend service")
                .Given(FriendServiceAcceptsTheRequest)
                .WhenAsync(SendingAFriendRequest)
                .Then(ResultShouldBe_, (object)LanguageKey.FRIEND_ADDED)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlacklistRequestReachesTheBlacklistService()
        {
            await new Spec("Blacklist request reaches the blacklist service")
                .Given(BlacklistServiceAcceptsTheRequest)
                .WhenAsync(SendingABlacklistRequest)
                .Then(ResultShouldBe_, (object)LanguageKey.BLACKLIST_ADDED)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WarehouseDepositReachesTheWarehouseService()
        {
            await new Spec("Warehouse deposit reaches the warehouse service")
                .Given(WarehouseServiceAcceptsTheDeposit)
                .WhenAsync(DepositingAnItem)
                .Then(ResultShouldBe_, (object)true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MailRequestReachesTheMailService()
        {
            await new Spec("Mail request reaches the mail service")
                .Given(MailServiceAcceptsTheMail)
                .WhenAsync(SendingAMail)
                .Then(ResultShouldBe_, (object)true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BazaarListingReachesTheBazaarService()
        {
            await new Spec("Bazaar listing reaches the bazaar service")
                .Given(BazaarServiceAcceptsTheListing)
                .WhenAsync(ListingAnItem)
                .Then(ResultShouldBe_, (object)LanguageKey.OBJECT_IN_BAZAAR)
                .ExecuteAsync();
        }

        private void FriendServiceAcceptsTheRequest() =>
            FriendService.Setup(s => s.AddFriendAsync(CharacterId, TargetId, FinsPacketType.Accepted))
                .ReturnsAsync(LanguageKey.FRIEND_ADDED);

        private void BlacklistServiceAcceptsTheRequest() =>
            BlacklistService.Setup(s => s.BlacklistPlayerAsync(CharacterId, TargetId)).ReturnsAsync(LanguageKey.BLACKLIST_ADDED);

        private void WarehouseServiceAcceptsTheDeposit() =>
            WarehouseService.Setup(s => s.DepositItemAsync(CharacterId, WarehouseType.FamilyWareHouse, Item, 4)).ReturnsAsync(true);

        private void MailServiceAcceptsTheMail() =>
            MailService.Setup(s => s.SendMailAsync(Mail, 1012, 5, 2, 3)).ReturnsAsync(true);

        private void BazaarServiceAcceptsTheListing() =>
            BazaarService.Setup(s => s.AddBazaarAsync(OperationId, Item.Id, CharacterId, "seller", true, 500, true, 7, 10))
                .ReturnsAsync(LanguageKey.OBJECT_IN_BAZAAR);

        private async Task SendingAFriendRequest() =>
            Result = await new FriendHub(FriendService.Object).AddFriendAsync(new FriendShipRequest
            {
                CharacterId = CharacterId,
                FinsPacket = new FinsPacket { CharacterId = TargetId, Type = FinsPacketType.Accepted }
            });

        private async Task SendingABlacklistRequest() =>
            Result = await new BlacklistHub(BlacklistService.Object).AddBlacklistAsync(new BlacklistRequest
            {
                CharacterId = CharacterId,
                BlInsPacket = new BlInsPacket { CharacterId = TargetId }
            });

        private async Task DepositingAnItem() =>
            Result = await new WarehouseHub(WarehouseService.Object).AddWarehouseItemAsync(new WareHouseDepositRequest
            {
                OwnerId = CharacterId,
                WarehouseType = WarehouseType.FamilyWareHouse,
                ItemInstance = Item,
                Slot = 4
            });

        private async Task SendingAMail() =>
            Result = await new MailHub(MailService.Object).SendMailAsync(new MailRequest
            {
                Mail = Mail,
                VNum = 1012,
                Amount = 5,
                Rare = 2,
                Upgrade = 3
            });

        private async Task ListingAnItem() =>
            Result = await new BazaarHub(BazaarService.Object).AddBazaarAsync(new BazaarRequest
            {
                OperationId = OperationId,
                ItemInstanceId = Item.Id,
                CharacterId = CharacterId,
                CharacterName = "seller",
                HasMedal = true,
                Price = 500,
                IsPackage = true,
                Duration = 7,
                Amount = 10
            });

        private void ResultShouldBe_(object expected) => Assert.AreEqual(expected, Result);
    }
}

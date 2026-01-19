//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using NosCore.Tests.Shared.BDD.Steps;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CRegPacketHandlerTests : SpecBase
    {
        private Mock<IBazaarHub> BazaarHttpClient = null!;
        private CRegPacketHandler CregPacketHandler = null!;
        private Mock<IDao<InventoryItemInstanceDto, Guid>> InventoryItemInstanceDao = null!;
        private Mock<IDao<IItemInstanceDto?, Guid>> ItemInstanceDao = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            BazaarHttpClient = new Mock<IBazaarHub>();
            InventoryItemInstanceDao = new Mock<IDao<InventoryItemInstanceDto, Guid>>();
            ItemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            BazaarHttpClient.Setup(s => s.AddBazaarAsync(It.IsAny<BazaarRequest>()))
                .ReturnsAsync(LanguageKey.OBJECT_IN_BAZAAR);
            CregPacketHandler = new CRegPacketHandler(
                TestHelpers.Instance.WorldConfiguration,
                BazaarHttpClient.Object,
                ItemInstanceDao.Object,
                InventoryItemInstanceDao.Object);
            ItemInstanceDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                .Returns<IItemInstanceDto?>(Task.FromResult);
        }

        [TestMethod]
        public async Task RegisteringWhileInShopShouldBeIgnored()
        {
            await new Spec("Registering while in shop should be ignored")
                .Given(TheCharacterIsInAShop)
                .WhenAsync(AttemptingToRegisterAnItem)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringWithoutEnoughGoldForTaxShouldFail()
        {
            await new Spec("Registering without enough gold for tax should fail")
                .WhenAsync(AttemptingToRegisterAnItem)
                .Then(ShouldReceiveNotEnoughGoldMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringNegativeAmountShouldBeIgnored()
        {
            await new Spec("Registering negative amount should be ignored")
                .Given(CharacterHasGold_, 500000L)
                .WhenAsync(AttemptingToRegisterNegativeAmount)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringNonExistentItemShouldBeIgnored()
        {
            await new Spec("Registering non existent item should be ignored")
                .Given(CharacterHasGold_, 500000L)
                .WhenAsync(AttemptingToRegisterAnItem)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringTooExpensiveWithoutMedalShouldFail()
        {
            await new Spec("Registering too expensive without medal should fail")
                .Given(CharacterHasGold_, 500000L)
                .And(CharacterHasItem_, (short)1012)
                .WhenAsync(AttemptingToRegisterAtExcessivePrice)
                .Then(ShouldReceivePriceLimitMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MedalHoldersPayReducedTax()
        {
            await new Spec("Medal holders pay reduced tax")
                .Given(CharacterHasGold_, 100000L)
                .And(CharacterHasBazaarMedal)
                .And(CharacterHasItem_, (short)1012)
                .WhenAsync(RegisteringAnExpensiveItem)
                .Then(InventoryShouldBeEmpty)
                .And(ShouldReceiveSuccessMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringOverMaxPriceShouldFail()
        {
            await new Spec("Registering over max price should fail")
                .Given(CharacterHasGold_, 5000000L)
                .And(CharacterHasItem_, (short)1012)
                .WhenAsync(AttemptingToRegisterAboveMaxGold)
                .Then(ShouldReceivePriceLimitMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringTooLongWithoutMedalShouldFail()
        {
            await new Spec("Registering too long without medal should fail")
                .Given(CharacterHasGold_, 5000000L)
                .And(CharacterHasItem_, (short)1012)
                .WhenAsync(AttemptingToRegisterForExtendedDuration)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringInvalidDurationShouldFail()
        {
            await new Spec("Registering invalid duration should fail")
                .Given(CharacterHasGold_, 5000000L)
                .And(CharacterHasItem_, (short)1012)
                .WhenAsync(AttemptingToRegisterWithInvalidDuration)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExceedingListingLimitShouldFail()
        {
            BazaarHttpClient.Reset();
            BazaarHttpClient.Setup(s => s.AddBazaarAsync(It.IsAny<BazaarRequest>()))
                .ReturnsAsync(LanguageKey.LIMIT_EXCEEDED);

            await new Spec("Exceeding listing limit should fail")
                .Given(CharacterHasGold_, 5000000L)
                .And(CharacterHasManyItems)
                .WhenAsync(AttemptingToRegisterBeyondLimit)
                .Then(ItemShouldRemainInInventory)
                .And(ShouldReceiveLimitExceededMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringFullStackShouldSucceed()
        {
            await new Spec("Registering full stack should succeed")
                .Given(CharacterHasGold_, 5000000L)
                .And(CharacterHasManyItems)
                .WhenAsync(RegisteringAll_Items, 999)
                .Then(InventoryShouldBeEmpty)
                .And(ShouldReceiveSuccessMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringMoreThanInventoryShouldFail()
        {
            await new Spec("Registering more than inventory should fail")
                .Given(CharacterHasGold_, 5000000L)
                .And(CharacterHasItem_, (short)1012)
                .WhenAsync(AttemptingToRegister_Items, 2)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringPartialStackShouldSucceed()
        {
            await new Spec("Registering partial stack should succeed")
                .Given(CharacterHasGold_, 5000000L)
                .And(CharacterHasManyItems)
                .WhenAsync(Registering_Items, 949)
                .Then(FiftyItemsShouldRemain)
                .And(ShouldReceiveSuccessMessage)
                .ExecuteAsync();
        }

        private CRegPacket CreateCRegPacket(short amount = 1, long price = 1000, byte durability = 1)
        {
            return new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Amount = amount,
                Price = price,
                Durability = durability,
                IsPackage = false,
                Taxe = 0,
                MedalUsed = 0
            };
        }

        private void TheCharacterIsInAShop()
        {
            Session.InShop();
        }

        private async Task AttemptingToRegisterAnItem()
        {
            await Session.HandlePacketsAsync(new[] { CreateCRegPacket() });
        }


        private void ShouldReceiveNotEnoughGoldMessage()
        {
            ShouldReceiveMessage(Game18NConstString.NotEnoughGold);
        }

        private void CharacterHasGold_(long gold)
        {
            CharacterHasGold(gold);
        }

        private async Task AttemptingToRegisterNegativeAmount()
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(amount: -1), Session);
        }

        private void CharacterHasItem_(short vnum)
        {
            CharacterHasItem(vnum);
        }

        private async Task AttemptingToRegisterAtExcessivePrice()
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(price: 100000001), Session);
        }

        private void ShouldReceivePriceLimitMessage()
        {
            ShouldReceiveModalMessage(Game18NConstString.NotExceedMaxPrice, 1, 4);
        }

        private void CharacterHasBazaarMedal()
        {
            CharacterHasMedalBonus(StaticBonusType.BazaarMedalGold);
        }

        private async Task RegisteringAnExpensiveItem()
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(price: 10000000), Session);
        }


        private void ShouldReceiveSuccessMessage()
        {
            ShouldReceiveMessage(Game18NConstString.ItemAddedToBazar, MessageType.Default);
        }

        private async Task AttemptingToRegisterAboveMaxGold()
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(price: TestHelpers.Instance.WorldConfiguration.Value.MaxGoldAmount + 1), Session);
        }

        private async Task AttemptingToRegisterForExtendedDuration()
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(price: 1, durability: 2), Session);
        }

        private async Task AttemptingToRegisterWithInvalidDuration()
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(price: 1, durability: 7), Session);
        }

        private void CharacterHasManyItems()
        {
            CharacterHasItem(1012, 999);
        }

        private async Task AttemptingToRegisterBeyondLimit()
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(amount: 949, price: 1), Session);
        }

        private void ItemShouldRemainInInventory()
        {
            InventoryShouldContainItem(1012, 999);
        }

        private void ShouldReceiveLimitExceededMessage()
        {
            ShouldReceiveModalMessage(Game18NConstString.ListedMaxItemsNumber);
        }

        private async Task RegisteringAll_Items(int value)
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(amount: 999, price: 1), Session);
        }

        private async Task AttemptingToRegister_Items(int value)
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(amount: 2, price: 1), Session);
        }

        private async Task Registering_Items(int value)
        {
            await CregPacketHandler.ExecuteAsync(CreateCRegPacket(amount: 949, price: 1), Session);
        }

        private void FiftyItemsShouldRemain()
        {
            InventoryShouldContainItem(1012, 50);
        }
    }
}

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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class SelectPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private SelectPacketHandler SelectPacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            await Session.SetCharacterAsync(null);
            SelectPacketHandler = new SelectPacketHandler(
                TestHelpers.Instance.CharacterDao,
                Logger,
                new Mock<IItemGenerationService>().Object,
                TestHelpers.Instance.MapInstanceAccessorService,
                new Mock<IDao<IItemInstanceDto?, Guid>>().Object,
                new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object,
                new Mock<IDao<StaticBonusDto, long>>().Object,
                new Mock<IDao<QuicklistEntryDto, Guid>>().Object,
                new Mock<IDao<TitleDto, Guid>>().Object,
                new Mock<IDao<CharacterQuestDto, Guid>>().Object,
                new Mock<IDao<ScriptDto, Guid>>().Object,
                new List<QuestDto>(),
                new List<QuestObjectiveDto>(),
                TestHelpers.Instance.WorldConfiguration,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.PubSubHub.Object);
        }

        [TestMethod]
        public async Task SelectingValidSlotShouldSelectCharacter()
        {
            await new Spec("Selecting valid slot should select character")
                .WhenAsync(SelectingCharacterInSlot_, 1)
                .Then(CharacterShouldBeSelected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SelectingEmptySlotShouldFail()
        {
            await new Spec("Selecting empty slot should fail")
                .WhenAsync(SelectingCharacterInSlot_, 0)
                .Then(CharacterShouldNotBeSelected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SelectingInvalidSlotShouldFail()
        {
            await new Spec("Selecting invalid slot should fail")
                .WhenAsync(SelectingCharacterInSlot_, 5)
                .Then(CharacterShouldNotBeSelected)
                .ExecuteAsync();
        }

        private async Task SelectingCharacterInSlot_(int slot)
        {
            var packet = new SelectPacket
            {
                Slot = (byte)slot
            };
            await SelectPacketHandler.ExecuteAsync(packet, Session);
        }

        private void CharacterShouldBeSelected()
        {
            Assert.IsTrue(Session.HasSelectedCharacter);
        }

        private void CharacterShouldNotBeSelected()
        {
            Assert.IsFalse(Session.HasSelectedCharacter);
        }
    }
}

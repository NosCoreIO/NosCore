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
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharRenPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Character Chara = null!;
        private CharRenPacketHandler CharRenPacketHandler = null!;
        private ClientSession Session = null!;
        private Mock<IMapChangeService> MapChangeService = null!;
        private const string NewCharacterName = "TestCharacter2";

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            MapChangeService = new Mock<IMapChangeService>();
            CharRenPacketHandler =
                new CharRenPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.WorldConfiguration);
            Session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler>
            {
                CharRenPacketHandler
            });
            Chara = Session.Character;
            Chara.ShouldRename = true;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(Session.Character);
            await Session.SetCharacterAsync(null);
        }

        [TestMethod]
        public async Task RenamingCharacterWhenInGameShouldFail()
        {
            await new Spec("Renaming character when in game should fail")
                .GivenAsync(CharacterIsInGame)
                .WhenAsync(RenamingCharacterViaPacket)
                .ThenAsync(CharacterShouldNotBeRenamed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RenamingCharacterShouldSucceed()
        {
            await new Spec("Renaming character should succeed")
                .WhenAsync(RenamingCharacter)
                .ThenAsync(CharacterShouldBeRenamed)
                .AndAsync(ShouldRenameFlagShouldBeCleared)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RenamingUnflaggedCharacterShouldFail()
        {
            await new Spec("Renaming unflagged character should fail")
                .GivenAsync(CharacterIsNotFlaggedForRename)
                .WhenAsync(RenamingCharacter)
                .ThenAsync(CharacterShouldNotBeRenamed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RenamingNonExistentCharacterShouldFail()
        {
            await new Spec("Renaming non existent character should fail")
                .WhenAsync(RenamingCharacterInWrongSlot)
                .ThenAsync(CharacterShouldNotBeRenamed)
                .ExecuteAsync();
        }

        private async Task CharacterIsInGame()
        {
            var idServer = new IdService<MapItem>(1);
            await Session.SetCharacterAsync(Chara);
            Session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                    new MapItemGenerationService(new EventLoaderService<MapItem, Tuple<MapItem, GetPacket>, IGetMapItemEventHandler>(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()), idServer),
                    Logger, TestHelpers.Instance.Clock, MapChangeService.Object, new Mock<ISessionGroupFactory>().Object, TestHelpers.Instance.SessionRegistry);
        }

        private async Task CharacterIsNotFlaggedForRename()
        {
            Chara.ShouldRename = false;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(Chara);
        }

        private async Task RenamingCharacterViaPacket()
        {
            await Session.HandlePacketsAsync(new[]
            {
                new CharRenamePacket
                {
                    Name = NewCharacterName,
                    Slot = 1
                }
            });
        }

        private async Task RenamingCharacter()
        {
            await CharRenPacketHandler.ExecuteAsync(new CharRenamePacket
            {
                Name = NewCharacterName,
                Slot = 1
            }, Session);
        }

        private async Task RenamingCharacterInWrongSlot()
        {
            await CharRenPacketHandler.ExecuteAsync(new CharRenamePacket
            {
                Name = NewCharacterName,
                Slot = 2
            }, Session);
        }

        private async Task CharacterShouldNotBeRenamed()
        {
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == NewCharacterName));
        }

        private async Task CharacterShouldBeRenamed()
        {
            var character = await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == NewCharacterName);
            Assert.IsNotNull(character);
        }

        private async Task ShouldRenameFlagShouldBeCleared()
        {
            var character = await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == NewCharacterName);
            Assert.IsFalse(character!.ShouldRename);
        }
    }
}

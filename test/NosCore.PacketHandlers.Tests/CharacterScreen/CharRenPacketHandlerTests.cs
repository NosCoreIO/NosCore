//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Infastructure;
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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharRenPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private CharRenPacketHandler CharRenPacketHandler = null!;
        private ClientSession Session = null!;
        private Data.Dto.CharacterDto ExistingCharacter = null!;
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
            ExistingCharacter = Session.Character.CharacterDto;
            ExistingCharacter.ShouldRename = true;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(ExistingCharacter);
            Session.ClearPlayerEntity();
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
            Session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler> { CharRenPacketHandler });
        }

        private async Task CharacterIsNotFlaggedForRename()
        {
            ExistingCharacter.ShouldRename = false;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(ExistingCharacter);
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

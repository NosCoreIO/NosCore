//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Encryption;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharacterDeletePacketHandlerTests
    {
        private TestWorld World;
        private CharacterDeletePacketHandler CharacterDeletePacketHandler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            CharacterDeletePacketHandler =
                new CharacterDeletePacketHandler(World.CharacterDao, World.AccountDao, new Sha512Hasher(), World.WorldConfiguration);
            Session = await World.CreateSessionAsync(new List<IPacketHandler>
            {
                CharacterDeletePacketHandler
            });
        }

        [TestMethod]
        public async Task DeletingCharacterWithInvalidPasswordShouldFail()
        {
            await new Spec("Deleting character with invalid password should fail")
                .GivenAsync(CharacterIsNotInGame)
                .WhenAsync(DeletingCharacterWithInvalidPassword)
                .ThenAsync(CharacterShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingCharacterWithTheAccountNameShouldSucceed()
        {
            await new Spec("Deleting character with the account name should succeed")
                .GivenAsync(CharacterIsNotInGame)
                .WhenAsync(DeletingCharacterWithTheAccountName)
                .ThenAsync(CharacterShouldBeDeleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingCharacterWithoutAPasswordShouldFail()
        {
            await new Spec("Deleting character without a password should fail")
                .GivenAsync(CharacterIsNotInGame)
                .WhenAsync(DeletingCharacterWithoutAPassword)
                .ThenAsync(CharacterShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingCharacterWhenInGameShouldFail()
        {
            await new Spec("Deleting character when in game should fail")
                .WhenAsync(DeletingCharacterViaPacket)
                .ThenAsync(CharacterShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingCharacterWithValidPasswordShouldSucceed()
        {
            await new Spec("Deleting character with valid password should succeed")
                .GivenAsync(CharacterIsNotInGame)
                .WhenAsync(DeletingCharacterWithValidPassword)
                .ThenAsync(CharacterShouldBeDeleted)
                .ExecuteAsync();
        }

        private Task CharacterIsNotInGame()
        {
            Session.ClearPlayerEntity();
            return Task.CompletedTask;
        }

        private async Task DeletingCharacterWithInvalidPassword()
        {
            await CharacterDeletePacketHandler.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "testpassword"
            }, Session);
        }

        private async Task DeletingCharacterWithTheAccountName()
        {
            await CharacterDeletePacketHandler.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = Session.Account.Name
            }, Session);
        }

        private async Task DeletingCharacterWithoutAPassword()
        {
            await CharacterDeletePacketHandler.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = null
            }, Session);
        }

        private async Task DeletingCharacterViaPacket()
        {
            await Session.HandlePacketsAsync(new[]
            {
                new CharacterDeletePacket
                {
                    Slot = 1,
                    Password = "test"
                }
            });
        }

        private async Task DeletingCharacterWithValidPassword()
        {
            await CharacterDeletePacketHandler.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, Session);
        }

        private async Task CharacterShouldStillExist()
        {
            Assert.IsNotNull(
                await World.CharacterDao
                    .FirstOrDefaultAsync(s =>
                        (s.AccountId == Session.Account.AccountId) && (s.State == CharacterState.Active)));
        }

        private async Task CharacterShouldBeDeleted()
        {
            Assert.IsNull(
                await World.CharacterDao
                    .FirstOrDefaultAsync(s =>
                        (s.AccountId == Session.Account.AccountId) && (s.State == CharacterState.Active)));
        }
    }
}

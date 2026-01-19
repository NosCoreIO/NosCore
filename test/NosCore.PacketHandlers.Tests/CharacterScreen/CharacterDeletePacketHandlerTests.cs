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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Encryption;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharacterDeletePacketHandlerTests
    {
        private CharacterDeletePacketHandler CharacterDeletePacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            CharacterDeletePacketHandler =
                new CharacterDeletePacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao, new Sha512Hasher(), TestHelpers.Instance.WorldConfiguration);
            Session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler>
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

        private async Task CharacterIsNotInGame()
        {
            await Session.SetCharacterAsync(null);
        }

        private async Task DeletingCharacterWithInvalidPassword()
        {
            await CharacterDeletePacketHandler.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "testpassword"
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
                await TestHelpers.Instance.CharacterDao
                    .FirstOrDefaultAsync(s =>
                        (s.AccountId == Session.Account.AccountId) && (s.State == CharacterState.Active)));
        }

        private async Task CharacterShouldBeDeleted()
        {
            Assert.IsNull(
                await TestHelpers.Instance.CharacterDao
                    .FirstOrDefaultAsync(s =>
                        (s.AccountId == Session.Account.AccountId) && (s.State == CharacterState.Active)));
        }
    }
}

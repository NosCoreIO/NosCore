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
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharacterDeletePacketHandlerTests
    {
        private CharacterDeletePacketHandler? _characterDeletePacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _characterDeletePacketHandler =
                new CharacterDeletePacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao, new Sha512Hasher(), TestHelpers.Instance.WorldConfiguration);
            _session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler>{
                _characterDeletePacketHandler
            }).ConfigureAwait(false);

        }

        [TestMethod]
        public async Task DeleteCharacter_Invalid_PasswordAsync()
        {
            await _session!.SetCharacterAsync(null).ConfigureAwait(false);
            await _characterDeletePacketHandler!.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "testpassword"
            }, _session).ConfigureAwait(false);
            Assert.IsNotNull(
                await TestHelpers.Instance.CharacterDao
                    .FirstOrDefaultAsync(s =>
                        (s.AccountId == _session.Account.AccountId) && (s.State == CharacterState.Active)).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task DeleteCharacterWhenInGame_Does_Not_Delete_CharacterAsync()
        {
            await _session!.HandlePacketsAsync(new[]{new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }}).ConfigureAwait(false);
            Assert.IsNotNull(
                await TestHelpers.Instance.CharacterDao
                    .FirstOrDefaultAsync(s =>
                        (s.AccountId == _session!.Account.AccountId) && (s.State == CharacterState.Active)).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task DeleteCharacterAsync()
        {
            await _session!.SetCharacterAsync(null).ConfigureAwait(false);
            await _characterDeletePacketHandler!.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session).ConfigureAwait(false);
            Assert.IsNull(
                await TestHelpers.Instance.CharacterDao
                    .FirstOrDefaultAsync(s =>
                        (s.AccountId == _session.Account.AccountId) && (s.State == CharacterState.Active)).ConfigureAwait(false));
        }
    }
}
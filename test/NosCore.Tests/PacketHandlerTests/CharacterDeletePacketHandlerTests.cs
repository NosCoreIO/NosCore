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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharacterDeletePacketHandlerTests
    {
        private CharacterDeletePacketHandler? _characterDeletePacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public void Setup()
        {
            new Mapper();
            _session = TestHelpers.Instance.GenerateSession();
            _characterDeletePacketHandler =
                new CharacterDeletePacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao);
        }

        [TestMethod]
        public async Task DeleteCharacter_Invalid_Password()
        {
            _session!.SetCharacter(null);
            await _characterDeletePacketHandler!.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "testpassword"
            }, _session).ConfigureAwait(false);
            Assert.IsNotNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s =>
                        (s.AccountId == _session.Account.AccountId) && (s.State == CharacterState.Active)));
        }

        [TestMethod]
        public async Task DeleteCharacterWhenInGame_Does_Not_Delete_Character()
        {
            await _characterDeletePacketHandler!.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session!).ConfigureAwait(false);
            Assert.IsNotNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s =>
                        (s.AccountId == _session!.Account.AccountId) && (s.State == CharacterState.Active)));
        }

        [TestMethod]
        public async Task DeleteCharacter()
        {
            _session!.SetCharacter(null);
            await _characterDeletePacketHandler!.ExecuteAsync(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session).ConfigureAwait(false);
            Assert.IsNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s =>
                        (s.AccountId == _session.Account.AccountId) && (s.State == CharacterState.Active)));
        }
    }
}
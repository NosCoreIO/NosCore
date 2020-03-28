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

using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharNewJobPacketHandlerTests
    {
        private Character _chara;

        private CharNewJobPacketHandler _charNewJobPacketHandler;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _chara = _session.Character;
            _session.SetCharacter(null);
            _charNewJobPacketHandler = new CharNewJobPacketHandler(TestHelpers.Instance.CharacterDao);
        }

        [TestMethod]
        public void CreateMartialArtistWhenNoLevel80_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
            Assert.IsNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateMartialArtist_Works()
        {
            const string name = "TestCharacter";
            _chara.Level = 80;
            CharacterDto character = _chara;
            TestHelpers.Instance.CharacterDao.InsertOrUpdate(ref character);
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
            Assert.IsNotNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateMartialArtistWhenAlreadyOne_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _chara.Class = CharacterClassType.MartialArtist;
            CharacterDto character = _chara;
            _chara.Level = 80;
            TestHelpers.Instance.CharacterDao.InsertOrUpdate(ref character);
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
            Assert.IsNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }
    }
}
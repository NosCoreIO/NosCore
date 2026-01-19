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
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.GameObject.ComponentEntities.Entities;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharNewJobPacketHandlerTests
    {
        private Character? _chara;

        private CharNewJobPacketHandler? _charNewJobPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _chara = _session.Character;
            await _session.SetCharacterAsync(null);
            TypeAdapterConfig<CharacterDto, Character>.NewConfig().ConstructUsing(src => _chara);
            _charNewJobPacketHandler = new CharNewJobPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.WorldConfiguration);
        }

        [TestMethod]
        public async Task CreateMartialArtistWhenNoLevel80_Does_Not_Create_CharacterAsync()
        {
            const string name = "TestCharacter";
            await _charNewJobPacketHandler!.ExecuteAsync(new CharNewJobPacket
            {
                Name = name
            }, _session!);
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name));
        }

        [TestMethod]
        public async Task CreateMartialArtist_WorksAsync()
        {
            const string name = "TestCharacter";
            _chara!.Level = 80;
            CharacterDto character = _chara;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(character);
            await _charNewJobPacketHandler!.ExecuteAsync(new CharNewJobPacket
            {
                Name = name
            }, _session!);
            Assert.IsNotNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name));
        }

        [TestMethod]
        public async Task CreateMartialArtistWhenAlreadyOne_Does_Not_Create_CharacterAsync()
        {
            const string name = "TestCharacter";
            _chara!.Class = CharacterClassType.MartialArtist;
            CharacterDto character = _chara;
            _chara.Level = 80;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(character);
            await _charNewJobPacketHandler!.ExecuteAsync(new CharNewJobPacket
            {
                Name = name
            }, _session!);
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name));
        }
    }
}
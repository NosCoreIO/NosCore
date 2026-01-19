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
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharNewJobPacketHandlerTests
    {
        private Character Chara = null!;
        private CharNewJobPacketHandler CharNewJobPacketHandler = null!;
        private ClientSession Session = null!;
        private const string TestCharacterName = "TestCharacter";

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Chara = Session.Character;
            await Session.SetCharacterAsync(null);
            TypeAdapterConfig<CharacterDto, Character>.NewConfig().ConstructUsing(src => Chara);
            CharNewJobPacketHandler = new CharNewJobPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.WorldConfiguration);
        }

        [TestMethod]
        public async Task CreatingMartialArtistWithoutLevel80ShouldFail()
        {
            await new Spec("Creating martial artist without level 80 should fail")
                .WhenAsync(CreatingMartialArtistAsync)
                .ThenAsync(CharacterShouldNotExistAsync)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingMartialArtistWithLevel80ShouldSucceed()
        {
            await new Spec("Creating martial artist with level 80 should succeed")
                .GivenAsync(CharacterIsLevel_Async, 80)
                .WhenAsync(CreatingMartialArtistAsync)
                .ThenAsync(CharacterShouldExistAsync)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingMartialArtistWhenAlreadyOneShouldFail()
        {
            await new Spec("Creating martial artist when already one should fail")
                .GivenAsync(CharacterIsAlreadyMartialArtistAsync)
                .WhenAsync(CreatingMartialArtistAsync)
                .ThenAsync(CharacterShouldNotExistAsync)
                .ExecuteAsync();
        }

        private async Task CharacterIsLevel_Async(int level)
        {
            Chara.Level = (byte)level;
            CharacterDto character = Chara;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(character);
        }

        private async Task CharacterIsAlreadyMartialArtistAsync()
        {
            Chara.Class = CharacterClassType.MartialArtist;
            Chara.Level = 80;
            CharacterDto character = Chara;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(character);
        }

        private async Task CreatingMartialArtistAsync()
        {
            await CharNewJobPacketHandler.ExecuteAsync(new CharNewJobPacket
            {
                Name = TestCharacterName
            }, Session);
        }

        private async Task CharacterShouldNotExistAsync()
        {
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName));
        }

        private async Task CharacterShouldExistAsync()
        {
            Assert.IsNotNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName));
        }
    }
}

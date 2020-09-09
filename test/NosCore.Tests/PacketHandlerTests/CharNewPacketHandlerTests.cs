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
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Drops;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharNewPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Character? _chara;
        private CharNewPacketHandler? _charNewPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _chara = _session.Character;
            await _session.SetCharacterAsync(null).ConfigureAwait(false);
            _charNewPacketHandler =
                new CharNewPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.MinilandDao);
        }

        [TestMethod]
        public async Task CreateCharacterWhenInGame_Does_Not_Create_CharacterAsync()
        {
            await _session!.SetCharacterAsync(_chara).ConfigureAwait(false);
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                    new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    Logger, new List<IMapInstanceEventHandler>());
            const string name = "TestCharacter";
            await _charNewPacketHandler!.ExecuteAsync(new CharNewPacket
            {
                Name = name
            }, _session).ConfigureAwait(false);
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task CreateCharacterAsync()
        {
            const string name = "TestCharacter";
            await _charNewPacketHandler!.ExecuteAsync(new CharNewPacket
            {
                Name = name
            }, _session!).ConfigureAwait(false);
            Assert.IsNotNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name).ConfigureAwait(false));
        }


        [TestMethod]
        public async Task InvalidName_Does_Not_Create_CharacterAsync()
        {
            const string name = "Test Character";
            await _charNewPacketHandler!.ExecuteAsync(new CharNewPacket
            {
                Name = name
            }, _session!).ConfigureAwait(false);
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ExistingName_Does_Not_Create_CharacterAsync()
        {
            const string name = "TestExistingCharacter";
            await _charNewPacketHandler!.ExecuteAsync(new CharNewPacket
            {
                Name = name
            }, _session!).ConfigureAwait(false);
            Assert.IsFalse(TestHelpers.Instance.CharacterDao.Where(s => s.Name == name)!.Skip(1).Any());
        }

        [TestMethod]
        public async Task NotEmptySlot_Does_Not_Create_CharacterAsync()
        {
            const string name = "TestCharacter";
            await _charNewPacketHandler!.ExecuteAsync(new CharNewPacket
            {
                Name = name,
                Slot = 1
            }, _session!).ConfigureAwait(false);
            Assert.IsFalse(TestHelpers.Instance.CharacterDao.Where(s => s.Slot == 1)!.Skip(1).Any());
        }
    }
}
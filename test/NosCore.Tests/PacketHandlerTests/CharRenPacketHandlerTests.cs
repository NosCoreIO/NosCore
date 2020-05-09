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
    public class CharRenPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Character? _chara;
        private CharRenPacketHandler? _charRenPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _chara = _session.Character;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(_session.Character);
            await _session.SetCharacterAsync(null).ConfigureAwait(false);
            _charRenPacketHandler =
                new CharRenPacketHandler(TestHelpers.Instance.CharacterDao);
        }

        [TestMethod]
        public async Task RenameCharacterWhenInGame_Does_Not_Rename_CharacterAsync()
        {
            await _session!.SetCharacterAsync(_chara).ConfigureAwait(false);
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                    new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    Logger, new List<IMapInstanceEventHandler>());
            const string name = "TestCharacter2";
            await _charRenPacketHandler!.ExecuteAsync(new CharRenamePacket
            {
                Name = name,
                Slot = 0
            }, _session).ConfigureAwait(false);
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task RenameCharacterAsync()
        {
            const string name = "TestCharacter2";
            await _charRenPacketHandler!.ExecuteAsync(new CharRenamePacket
            {
                Name = name,
                Slot = 1
            }, _session!).ConfigureAwait(false);
            Assert.IsNotNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task RenameNotFoundCharacterAsync()
        {
            const string name = "TestCharacter2";
            await _charRenPacketHandler!.ExecuteAsync(new CharRenamePacket
            {
                Name = name,
                Slot = 2
            }, _session!).ConfigureAwait(false);
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name).ConfigureAwait(false));
        }
    }
}
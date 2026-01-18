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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.Networking.SessionGroup;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.PathFinder.Interfaces;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Tests.Shared;
using Serilog;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.ShopService;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharRenPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private PlayerContext _player;
        private CharRenPacketHandler? _charRenPacketHandler;
        private ClientSession? _session;
        private Mock<IMapChangeService> _mapChangeService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _mapChangeService = new Mock<IMapChangeService>();
            _charRenPacketHandler =
                new CharRenPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.WorldConfiguration);
            _session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler>{
                _charRenPacketHandler
            }).ConfigureAwait(false);
            _player = _session.Player;
            _player.CharacterData.ShouldRename = true;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(_player.CharacterData);
            _session.ClearPlayer();

        }

        [TestMethod]
        public async Task RenameCharacterWhenInGame_Does_Not_Rename_CharacterAsync()
        {
            var idServer = new IdService<MapItemRef>(1);
            var mapInstance = new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                new MapItemGenerationService(new EventLoaderService<MapItemRef, Tuple<MapItemRef, GetPacket>, IGetMapItemEventHandler>(new List<IEventHandler<MapItemRef, Tuple<MapItemRef, GetPacket>>>()), idServer, new MapItemRegistry()),
                Logger, TestHelpers.Instance.Clock, _mapChangeService.Object, new Mock<ISessionGroupFactory>().Object, TestHelpers.Instance.SessionRegistry, new Mock<IHeuristic>().Object,
                new VisibilitySystem(), new MorphSystem(), new EntityPacketSystem(), new ShopRegistry());
            await _session!.SetPlayerAsync(_player.GameState, _player.CharacterData, mapInstance).ConfigureAwait(false);
            const string name = "TestCharacter2";
            await _session!.HandlePacketsAsync(new[] { new CharRenamePacket
            {
                Name = name,
                Slot = 1
            }}).ConfigureAwait(false);
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
            var character = await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name)
                .ConfigureAwait(false);
            Assert.IsNotNull(character);
            Assert.IsFalse(character.ShouldRename);
        }

        [TestMethod]
        public async Task RenameUnflaggedCharacterAsync()
        {
            const string name = "TestCharacter2";
            await _charRenPacketHandler!.ExecuteAsync(new CharRenamePacket
            {
                Name = name,
                Slot = 1
            }, _session!).ConfigureAwait(false);
            _player.CharacterData.ShouldRename = false;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(_player.CharacterData);
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == name).ConfigureAwait(false));
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
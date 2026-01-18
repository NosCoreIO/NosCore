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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Core.Services.IdService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.Networking.SessionGroup;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ItemGenerationService;
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
    public class CharNewPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private PlayerContext _player;
        private CharNewPacketHandler? _charNewPacketHandler;
        private ClientSession? _session;
        private Mock<IMapChangeService> _mapChangeService = null!;


        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _charNewPacketHandler =
                new CharNewPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.MinilandDao, new Mock<IItemGenerationService>().Object, new Mock<IDao<QuicklistEntryDto, Guid>>().Object,
                    new Mock<IDao<IItemInstanceDto?, Guid>>().Object, new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object, new HpService(), new MpService(), TestHelpers.Instance.WorldConfiguration, new Mock<IDao<CharacterSkillDto,Guid>>().Object, () => new Mock<NosCore.GameObject.Services.InventoryService.IInventoryService>().Object);
            _session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler> { _charNewPacketHandler }).ConfigureAwait(false);
            _player = _session.Player;
            _mapChangeService = new Mock<IMapChangeService>();
            _session.ClearPlayer();
        }

        [TestMethod]
        public async Task CreateCharacterWhenInGame_Does_Not_Create_CharacterAsync()
        {
            var idServer = new IdService<MapItemRef>(1);
            var mapInstance = new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                new MapItemGenerationService(new EventLoaderService<MapItemRef, Tuple<MapItemRef, GetPacket>, IGetMapItemEventHandler>(new List<IEventHandler<MapItemRef, Tuple<MapItemRef, GetPacket>>>()), idServer, new MapItemRegistry()),
                Logger, TestHelpers.Instance.Clock, _mapChangeService.Object, new Mock<ISessionGroupFactory>().Object, TestHelpers.Instance.SessionRegistry, new Mock<IHeuristic>().Object,
                new VisibilitySystem(), new MorphSystem(), new EntityPacketSystem(), new ShopRegistry());
            await _session!.SetPlayerAsync(_player.GameState, _player.CharacterData, mapInstance).ConfigureAwait(false);
            const string name = "TestCharacter";
            await _session!.HandlePacketsAsync(new[] {new CharNewPacket
            {
                Name = name
            }}).ConfigureAwait(false);
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
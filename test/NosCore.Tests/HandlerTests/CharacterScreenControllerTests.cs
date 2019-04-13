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
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ClientPackets.Drops;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;

using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using Serilog;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class CharacterScreenControllerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly List<NpcMonsterDto> _npcMonsters = new List<NpcMonsterDto>();

        private readonly ClientSession _session = new ClientSession(null,
            new List<PacketController> {new CharacterScreenPacketController()}, null, null, _logger);

        private Character _chara;
        private CharacterScreenPacketController _handler;

        private readonly IGenericDao<CharacterDto> _characterDao = new GenericDao<Database.Entities.Character, CharacterDto>(_logger);
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Database.Entities.Account, AccountDto>(_logger);
        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
        private readonly IGenericDao<MateDto> _mateDao = new GenericDao<Database.Entities.Mate, MateDto>(_logger);
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao(_logger);

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<CharacterDto, Character>.NewConfig().ConstructUsing(src => new Character(null, null, null, null, null, null, null, _logger));
            TypeAdapterConfig<MapMonsterDto, MapMonster>.NewConfig().ConstructUsing(src => new MapMonster(new List<NpcMonsterDto>(), _logger));
            new Mapper();
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto {MapId = 1};
            _mapDao.InsertOrUpdate(ref map);
            var _acc = new AccountDto {Name = "AccountTest", Password = "test".ToSha512()};
            _accountDao.InsertOrUpdate(ref _acc);
            _chara = new Character(null, null, null, _characterRelationDao, _characterDao, _itemInstanceDao, _accountDao, _logger)
            {
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = _acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };
            CharacterDto character = _chara;
            _characterDao.InsertOrUpdate(ref character);
            _session.InitializeAccount(_acc);
            _handler = new CharacterScreenPacketController(null, null, new Adapter(), _characterDao, _accountDao, _itemInstanceDao, _mateDao, _logger);
            _handler.RegisterSession(_session);
        }

        [TestMethod]
        public void CreateMartialArtistWhenNoLevel80_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _handler.CreateMartialArtist(new CharNewJobPacket()
            {
                Name = name
            });
            Assert.IsNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateMartialArtist_Works()
        {
            const string name = "TestCharacter";
            _chara.Level = 80;
            CharacterDto character = _chara;
            _characterDao.InsertOrUpdate(ref character);
            _handler.CreateMartialArtist(new CharNewJobPacket()
            {
                Name = name
            });
            Assert.IsNotNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateMartialArtistWhenAlreadyOne_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _chara.Class = CharacterClassType.MartialArtist;
            CharacterDto character = _chara;
            _chara.Level = 80;
            _characterDao.InsertOrUpdate(ref character);
            _handler.CreateMartialArtist(new CharNewJobPacket
            {
                Name = name
            });
            Assert.IsNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacterWhenInGame_Does_Not_Create_Character()
        {
            _session.SetCharacter(_chara);
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                    new MapItemProvider(new List<IHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    null, _logger);
            const string name = "TestCharacter";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter()
        {
            const string name = "TestCharacter";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNotNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter_With_Packet()
        {
            const string name = "TestCharacter";
            _handler.CreateCharacter(
                (CharNewPacket) PacketFactory.Deserialize($"Char_NEW {name} 0 0 0 0", typeof(CharNewPacket)));
            Assert.IsNotNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void InvalidName_Does_Not_Create_Character()
        {
            const string name = "Test Character";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void InvalidSlot_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            Assert.IsNull(PacketFactory.Deserialize($"Char_NEW {name} 4 0 0 0", typeof(CharNewPacket)));
        }

        [TestMethod]
        public void ExistingName_Does_Not_Create_Character()
        {
            const string name = "TestExistingCharacter";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsFalse(_characterDao.Where(s => s.Name == name).Skip(1).Any());
        }

        [TestMethod]
        public void NotEmptySlot_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name,
                Slot = 1
            });
            Assert.IsFalse(_characterDao.Where(s => s.Slot == 1).Skip(1).Any());
        }

        [TestMethod]
        public void DeleteCharacter_With_Packet()
        {
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter(
                (CharacterDeletePacket) PacketFactory.Deserialize("Char_DEL 1 test", typeof(CharacterDeletePacket)));
            Assert.IsNull(
                _characterDao
                    .FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacter_Invalid_Password()
        {
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter((CharacterDeletePacket) PacketFactory.Deserialize("Char_DEL 1 testpassword",
                typeof(CharacterDeletePacket)));
            Assert.IsNotNull(
                _characterDao
                    .FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacterWhenInGame_Does_Not_Delete_Character()
        {
            _session.SetCharacter(_chara);
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                    new MapItemProvider(new List<IHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    null, _logger);
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter(new CharacterDeletePacket
            {
                Password = "test",
                Slot = 1
            });
            Assert.IsNotNull(
                _characterDao
                    .FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacter()
        {
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter(new CharacterDeletePacket
            {
                Password = "test",
                Slot = 1
            });
            Assert.IsNull(
                _characterDao
                    .FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }
    }
}
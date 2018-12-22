//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.CharacterBuilder;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.GameObject.Services.MapItemBuilder;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class CharacterScreenControllerTests
    {
        private readonly List<NpcMonsterDto> _npcMonsters = new List<NpcMonsterDto>();

        private readonly ClientSession _session = new ClientSession(null,
            new List<PacketController> {new CharacterScreenPacketController()}, null);
        
        private CharacterDto _chara;
        private CharacterScreenPacketController _handler;

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto {MapId = 1};
            DaoFactory.MapDao.InsertOrUpdate(ref map);
            var _acc = new AccountDto {Name = "AccountTest", Password ="test".ToSha512()};
            DaoFactory.AccountDao.InsertOrUpdate(ref _acc);
            _chara = new CharacterDto
            {
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = _acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };
            DaoFactory.CharacterDao.InsertOrUpdate(ref _chara);
            _session.InitializeAccount(_acc);
            _handler = new CharacterScreenPacketController(new CharacterBuilderService(null), null, null);
            _handler.RegisterSession(_session);
        }

        [TestMethod]
        public void CreateCharacterWhenInGame_Does_Not_Create_Character()
        {
            _session.SetCharacter(_chara.Adapt<Character>());
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance, _npcMonsters, new MapItemBuilderService(new List<IHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    null, null);
            const string name = "TestCharacter";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNull(DaoFactory.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter()
        {
            const string name = "TestCharacter";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNotNull(DaoFactory.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter_With_Packet()
        {
            const string name = "TestCharacter";
            _handler.CreateCharacter(
                (CharNewPacket) PacketFactory.Deserialize($"Char_NEW {name} 0 0 0 0", typeof(CharNewPacket)));
            Assert.IsNotNull(DaoFactory.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void InvalidName_Does_Not_Create_Character()
        {
            const string name = "Test Character";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNull(DaoFactory.CharacterDao.FirstOrDefault(s => s.Name == name));
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
            Assert.IsFalse(DaoFactory.CharacterDao.Where(s => s.Name == name).Skip(1).Any());
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
            Assert.IsFalse(DaoFactory.CharacterDao.Where(s => s.Slot == 1).Skip(1).Any());
        }

        [TestMethod]
        public void DeleteCharacter_With_Packet()
        {
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter(
                (CharacterDeletePacket) PacketFactory.Deserialize("Char_DEL 1 test", typeof(CharacterDeletePacket)));
            Assert.IsNull(
                DaoFactory.CharacterDao.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacter_Invalid_Password()
        {
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter((CharacterDeletePacket) PacketFactory.Deserialize("Char_DEL 1 testpassword",
                typeof(CharacterDeletePacket)));
            Assert.IsNotNull(
                DaoFactory.CharacterDao.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacterWhenInGame_Does_Not_Delete_Character()
        {
            _session.SetCharacter(_chara.Adapt<Character>());
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance, _npcMonsters, new MapItemBuilderService(new List<IHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    null, null);
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter(new CharacterDeletePacket
            {
                Password = "test",
                Slot = 1
            });
            Assert.IsNotNull(
                DaoFactory.CharacterDao.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
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
                DaoFactory.CharacterDao.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }
    }
}
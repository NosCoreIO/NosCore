using System;
using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ClientPackets.Drops;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PacketHandlers.CharacterScreen;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharacterDeletePacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly ClientSession _session = new ClientSession(null, null, null, _logger, null);
        private Character _chara;

        private readonly IGenericDao<CharacterDto> _characterDao =
            new GenericDao<Database.Entities.Character, CharacterDto>(_logger);

        private readonly IGenericDao<AccountDto> _accountDao =
            new GenericDao<Database.Entities.Account, AccountDto>(_logger);

        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);

        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao =
            new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);

        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao(_logger);
        private CharacterDeletePacketHandler _characterDeletePacketHandler;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<CharacterDto, Character>.NewConfig().ConstructUsing(src =>
                new Character(null, null, null, null, null, null, null, _logger, null));
            TypeAdapterConfig<MapMonsterDto, MapMonster>.NewConfig()
                .ConstructUsing(src => new MapMonster(new List<NpcMonsterDto>(), _logger));
            new Mapper();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto {MapId = 1};
            _mapDao.InsertOrUpdate(ref map);
            var _acc = new AccountDto {Name = "AccountTest", Password = "test".ToSha512()};
            _accountDao.InsertOrUpdate(ref _acc);
            _chara = new Character(null, null, null, _characterRelationDao, _characterDao, _itemInstanceDao,
                _accountDao, _logger, null)
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
            _characterDeletePacketHandler = new CharacterDeletePacketHandler(_characterDao, _accountDao);
        }

        [TestMethod]
        public void DeleteCharacter_Invalid_Password()
        {
            const string name = "TestExistingCharacter";
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "testpassword"
            }, _session);
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
                    new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    null, _logger);
            const string name = "TestExistingCharacter";
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session);
            Assert.IsNotNull(
                _characterDao
                    .FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacter()
        {
            const string name = "TestExistingCharacter";
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session);
            Assert.IsNull(
                _characterDao
                    .FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }
    }
}
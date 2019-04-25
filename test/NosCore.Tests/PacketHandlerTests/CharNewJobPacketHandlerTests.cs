using System;
using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharNewJobPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private ClientSession _session;
        private Character _chara;

        private readonly IGenericDao<CharacterDto> _characterDao =
            new GenericDao<Database.Entities.Character, CharacterDto>(_logger);

        private readonly IGenericDao<AccountDto> _accountDao =
            new GenericDao<Database.Entities.Account, AccountDto>(_logger);

        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);

        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao =
            new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);

        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao(_logger);
        private CharNewJobPacketHandler _charNewJobPacketHandler;

        [TestInitialize]
        public void Setup()
        {
            _session = new ClientSession(new WorldConfiguration(), null, null, _logger, new[] { new CharNewPacketHandler(_characterDao) });
            TypeAdapterConfig<CharacterDto, Character>.NewConfig().ConstructUsing(src =>
                new Character(null, null, null, null, null, null, null, _logger, null));
            TypeAdapterConfig<MapMonsterDto, MapMonster>.NewConfig()
                .ConstructUsing(src => new MapMonster(new List<NpcMonsterDto>(), _logger));
            new Mapper();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            _mapDao.InsertOrUpdate(ref map);
            var _acc = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };
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
            _charNewJobPacketHandler = new CharNewJobPacketHandler(_characterDao);
        }

        [TestMethod]
        public void CreateMartialArtistWhenNoLevel80_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
            Assert.IsNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateMartialArtist_Works()
        {
            const string name = "TestCharacter";
            _chara.Level = 80;
            CharacterDto character = _chara;
            _characterDao.InsertOrUpdate(ref character);
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
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
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
            Assert.IsNull(_characterDao.FirstOrDefault(s => s.Name == name));
        }
    }
}
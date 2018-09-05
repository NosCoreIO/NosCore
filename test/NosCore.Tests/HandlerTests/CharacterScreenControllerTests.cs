using System;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class CharacterScreenControllerTests
    {
        private const string ConfigurationPath = "../../../configuration";
        private readonly ClientSession _session = new ClientSession(null, null);
        private AccountDTO _acc;
        private CharacterDTO _chara;
        private CharacterScreenPacketController _handler;

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var builder = new ConfigurationBuilder();
            var databaseConfiguration = new SqlConnectionConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("database.json", false);
            builder.Build().Bind(databaseConfiguration);
            databaseConfiguration.Database = "postgresunittest";
            var sqlconnect = databaseConfiguration;
            DataAccessHelper.Instance.EnsureDeleted(sqlconnect);
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(ConfigurationPath + "/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(CharacterScreenControllerTests)));
            DataAccessHelper.Instance.Initialize(sqlconnect);
            DAOFactory.RegisterMapping(typeof(Character).Assembly);
            var map = new MapDTO {MapId = 1};
            DAOFactory.MapDAO.InsertOrUpdate(ref map);
            _acc = new AccountDTO {Name = "AccountTest", Password = EncryptionHelper.Sha512("test")};
            DAOFactory.AccountDAO.InsertOrUpdate(ref _acc);
            _chara = new CharacterDTO
            {
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = _acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };
            DAOFactory.CharacterDAO.InsertOrUpdate(ref _chara);
            _session.InitializeAccount(_acc);
            _handler = new CharacterScreenPacketController();
            _handler.RegisterSession(_session);
        }

        [TestMethod]
        public void CreateCharacterWhenInGame_Does_Not_Create_Character()
        {
            _session.SetCharacter((Character) _chara);
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance);
            const string name = "TestCharacter";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter()
        {
            const string name = "TestCharacter";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNotNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter_With_Packet()
        {
            const string name = "TestCharacter";
            _handler.CreateCharacter(
                (CharNewPacket) PacketFactory.Deserialize($"Char_NEW {name} 0 0 0 0", typeof(CharNewPacket)));
            Assert.IsNotNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void InvalidName_Does_Not_Create_Character()
        {
            const string name = "Test Character";
            _handler.CreateCharacter(new CharNewPacket
            {
                Name = name
            });
            Assert.IsNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
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
            Assert.IsFalse(DAOFactory.CharacterDAO.Where(s => s.Name == name).Skip(1).Any());
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
            Assert.IsFalse(DAOFactory.CharacterDAO.Where(s => s.Slot == 1).Skip(1).Any());
        }

        [TestMethod]
        public void DeleteCharacter_With_Packet()
        {
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter(
                (CharacterDeletePacket) PacketFactory.Deserialize("Char_DEL 1 test", typeof(CharacterDeletePacket)));
            Assert.IsNull(
                DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacter_Invalid_Password()
        {
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter((CharacterDeletePacket) PacketFactory.Deserialize("Char_DEL 1 testpassword",
                typeof(CharacterDeletePacket)));
            Assert.IsNotNull(
                DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacterWhenInGame_Does_Not_Delete_Character()
        {
            _session.SetCharacter((Character) _chara);
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance);
            const string name = "TestExistingCharacter";
            _handler.DeleteCharacter(new CharacterDeletePacket
            {
                Password = "test",
                Slot = 1
            });
            Assert.IsNotNull(
                DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
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
                DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }
    }
}
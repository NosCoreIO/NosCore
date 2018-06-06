using AutoMapper;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.DAL;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.Shared.Character;
using NosCore.Shared.Map;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.Controllers;
using NosCore.Packets.ClientPackets;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NosCore.GameObject.Map;
using NosCore.Configuration;
using Microsoft.Extensions.Configuration;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class CharacterScreenPacketHandlerTests
    {
        private CharacterScreenPacketController handler;
        private readonly ClientSession session = new ClientSession();
        private AccountDTO acc;

        private const string _configurationPath = @"..\..\..\configuration";

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var builder = new ConfigurationBuilder();
            SqlConnectionConfiguration databaseConfiguration = new SqlConnectionConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("database.json", false);
            builder.Build().Bind(databaseConfiguration);
            databaseConfiguration.Database = "postgresunittest";
            var sqlconnect = databaseConfiguration;
            DataAccessHelper.Instance.EnsureDeleted(sqlconnect);
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Shared.Logger.Logger.InitializeLogger(LogManager.GetLogger(typeof(CharacterScreenPacketHandlerTests)));
            DataAccessHelper.Instance.Initialize(sqlconnect);
            DAOFactory.RegisterMapping(typeof(Character).Assembly);
            MapDTO map = new MapDTO() { MapId = 1 };
            DAOFactory.MapDAO.InsertOrUpdate(ref map);
            acc = new AccountDTO() { Name = "AccountTest", Password = EncryptionHelper.Sha512("test") };
            DAOFactory.AccountDAO.InsertOrUpdate(ref acc);
            CharacterDTO chara = new CharacterDTO() { Name = "TestExistingCharacter", Slot = 1, AccountId = acc.AccountId, MapId = 1, State = CharacterState.Active };
            DAOFactory.CharacterDAO.InsertOrUpdate(ref chara);
            session.InitializeAccount(acc);
            handler = new CharacterScreenPacketController();
            handler.RegisterSession(session);
        }

        [TestMethod]
        public void CreateCharacterWhenInGame_Does_Not_Create_Character()
        {
            session.CurrentMapInstance = new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance);
            const string name = "TestCharacter";
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name
            });
            Assert.IsNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter()
        {
            const string name = "TestCharacter";
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name
            });
            Assert.IsNotNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter_With_Packet()
        {
            const string name = "TestCharacter";
            handler.CreateCharacter((CharNewPacket)PacketFactory.Deserialize($"Char_NEW {name} 0 0 0 0", typeof(CharNewPacket)));
            Assert.IsNotNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void InvalidName_Does_Not_Create_Character()
        {
            const string name = "Test Character";
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name,
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
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name
            });
            Assert.IsFalse(DAOFactory.CharacterDAO.Where(s => s.Name == name).Skip(1).Any());
        }

        [TestMethod]
        public void NotEmptySlot_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            handler.CreateCharacter(new CharNewPacket()
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
            handler.DeleteCharacter((CharacterDeletePacket)PacketFactory.Deserialize($"Char_DEL 1 test", typeof(CharacterDeletePacket)));
            Assert.IsNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacter_Invalid_Password()
        {
            const string name = "TestExistingCharacter";
            handler.DeleteCharacter((CharacterDeletePacket)PacketFactory.Deserialize($"Char_DEL 1 testpassword", typeof(CharacterDeletePacket)));
            Assert.IsNotNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacterWhenInGame_Does_Not_Delete_Character()
        {
            session.CurrentMapInstance = new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance);
            const string name = "TestExistingCharacter";
            handler.DeleteCharacter(new CharacterDeletePacket()
            {
                Password = "test",
                Slot = 1
            });
            Assert.IsNotNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacter()
        {
            const string name = "TestExistingCharacter";
            handler.DeleteCharacter(new CharacterDeletePacket()
            {
                Password = "test",
                Slot = 1
            });
            Assert.IsNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name && s.State == CharacterState.Active));
        }
    }
}

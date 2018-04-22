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
using NosCore.Domain.Character;
using NosCore.Domain.Map;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.Handler;
using NosCore.Packets.ClientPackets;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class CharacterScreenPacketHandlerTests
    {
        private CharacterScreenPacketHandler handler;
        private readonly Mock<IClientSession> session = new Mock<IClientSession>();
        private AccountDTO acc;


        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var sqlconnect = new SqlConnectionStringBuilder(@"Server=localhost;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0");
            DataAccessHelper.Instance.EnsureDeleted(sqlconnect);
            session.Setup(s => s.SendPacket(It.IsAny<PacketDefinition>())).Verifiable();
            session.SetupAllProperties();
            session.Setup(s => s.InitializeAccount(It.IsAny<AccountDTO>())).Callback((AccountDTO acc) =>
            {
                session.Object.Account = acc;
                session.Object.IsAuthenticated = true;
            }).Verifiable();
            session.Setup(s => s.HasCurrentMapInstance).Returns(() => session.Object.CurrentMapInstance != null).Verifiable();
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Core.Logger.Logger.InitializeLogger(LogManager.GetLogger(typeof(CharacterScreenPacketHandlerTests)));
            DataAccessHelper.Instance.Initialize(sqlconnect);
            Mapping.Mapper.InitializeMapping();
            MapDTO map = new MapDTO() { MapId = 1 };
            DAOFactory.MapDAO.InsertOrUpdate(ref map);
            acc = new AccountDTO() { Name = "AccountTest", Password = EncryptionHelper.Sha512("test") };
            DAOFactory.AccountDAO.InsertOrUpdate(ref acc);
            CharacterDTO chara = new CharacterDTO() { Name = "TestExistingCharacter", Slot = 1, AccountId = acc.AccountId, MapId = 1, State = CharacterState.Active };
            DAOFactory.CharacterDAO.InsertOrUpdate(ref chara);
            session.Object.InitializeAccount(acc);
            handler = new CharacterScreenPacketHandler(session.Object);
        }

        [TestMethod]
        public void CreateCharacterWhenInGame_Does_Not_Create_Character()
        {
            session.Object.CurrentMapInstance = new MapInstance(new MapDTO(), new Guid(), true, MapInstanceType.BaseMapInstance);
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
            session.Object.CurrentMapInstance = new MapInstance(new MapDTO(), new Guid(), true, MapInstanceType.BaseMapInstance);
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

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
        private static void InitializeMapping()
        {
            MapperConfiguration config = new MapperConfiguration(cfg =>
            {
                foreach (Type type in typeof(CharacterDTO).Assembly.GetTypes().Where(t => typeof(IDTO).IsAssignableFrom(t)))
                {
                    int index = type.Name.LastIndexOf("DTO");
                    if (index >= 0)
                    {
                        string name = type.Name.Substring(0, index);
                        Type typefound = typeof(Character).Assembly.GetTypes().SingleOrDefault(t => t.Name.Equals(name));
                        Type entitytypefound = typeof(Database.Entities.Account).Assembly.GetTypes().SingleOrDefault(t => t.Name.Equals(name));
                        if (entitytypefound != null)
                        {
                            cfg.CreateMap(type, entitytypefound).ReverseMap();
                            if (typefound != null)
                            {
                                cfg.CreateMap(entitytypefound, type).As(typefound);
                            }
                        }
                    }
                }
            });
            DAOFactory.RegisterMapping(config.CreateMapper());
        }

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var sqlconnect = new SqlConnectionStringBuilder(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0");
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
            InitializeMapping();
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
            var name = "TestCharacter";
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name
            });
            Assert.IsNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter()
        {
            var name = "TestCharacter";
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name
            });
            Assert.IsNotNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter_With_Packet()
        {
            var name = "TestCharacter";
            handler.CreateCharacter((CharNewPacket)PacketFactory.Deserialize($"Char_NEW {name} 0 0 0 0", typeof(CharNewPacket)));
            Assert.IsNotNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }


        [TestMethod]
        public void InvalidName_Does_Not_Create_Character()
        {
            var name = "Test Character";
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name,
            });
            Assert.IsNull(DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void InvalidSlot_Does_Not_Create_Character()
        {
            var name = "TestCharacter";
            Assert.IsNull(PacketFactory.Deserialize($"Char_NEW {name} 4 0 0 0", typeof(CharNewPacket)));
        }

        [TestMethod]
        public void ExistingName_Does_Not_Create_Character()
        {
            var name = "TestExistingCharacter";
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name
            });
            Assert.IsFalse(DAOFactory.CharacterDAO.Where(s => s.Name == name).Skip(1).Any());
        }

        [TestMethod]
        public void NotEmptySlot_Does_Not_Create_Character()
        {
            var name = "TestCharacter";
            handler.CreateCharacter(new CharNewPacket()
            {
                Name = name,
                Slot = 1
            });
            Assert.IsFalse(DAOFactory.CharacterDAO.Where(s => s.Slot == 1).Skip(1).Any());
        }

    }
}

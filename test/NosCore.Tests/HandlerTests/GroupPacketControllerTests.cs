using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
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
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.I18N;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class GroupPacketControllerTests
    {
        private const string ConfigurationPath = "../../../configuration";
        private readonly ClientSession _session = new ClientSession(null, new List<PacketController>() { new GroupPacketController() });
        private AccountDTO _acc;
        private CharacterDTO _chara;
        private GroupPacketController _handler;

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
            Logger.InitializeLogger(LogManager.GetLogger(typeof(GroupPacketControllerTests)));
            DataAccessHelper.Instance.Initialize(sqlconnect);
            DAOFactory.RegisterMapping(typeof(Character).Assembly);
            var map = new MapDTO { MapId = 1 };
            DAOFactory.MapDAO.InsertOrUpdate(ref map);
            _acc = new AccountDTO { Name = "AccountTest", Password = EncryptionHelper.Sha512("test") };
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
            _handler = new GroupPacketController();
            _handler.RegisterSession(_session);
        }
    }
}

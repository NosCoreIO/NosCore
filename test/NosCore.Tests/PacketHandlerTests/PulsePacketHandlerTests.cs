using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using ChickenAPI.Packets.ServerPackets.UI;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapItemProvider.Handlers;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Game;
using NosCore.PacketHandlers.Login;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PulsePacketHandlerTests
    {
        private PulsePacketHandler _pulsePacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<CharacterDto> _characterDao = new GenericDao<Database.Entities.Character, CharacterDto>(_logger);
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Database.Entities.Account, AccountDto>(_logger);
        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao(_logger);


        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            _pulsePacketHandler = new PulsePacketHandler();
            var conf = new WorldConfiguration();

            Broadcaster.Reset();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            _mapDao.InsertOrUpdate(ref map);
            var account = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };
            _accountDao.InsertOrUpdate(ref account);
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<WebApiRoute, object>
                {
                    {WebApiRoute.Channel, new List<ChannelInfo> {new ChannelInfo()}},
                    {WebApiRoute.ConnectedAccount, new List<ConnectedAccount>()}
                };

            var _chara = new Character(null, null, null, _characterRelationDao, _characterDao, _itemInstanceDao, _accountDao, _logger, null)
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = account.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            var channelMock = new Mock<IChannel>();
            _session = new ClientSession(conf, null, null, _logger, new List<IPacketHandler>());
            _session.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;
            _session.SetCharacter(_chara);

            Broadcaster.Instance.RegisterSession(_session);
        }

        [TestMethod]
        public void Test_Pulse_Packet()
        {
            var pulsePacket = new PulsePacket
            {
                Tick = 0
            };

            for (var i = 60; i < 600; i += 60)
            {
                pulsePacket = new PulsePacket
                {
                    Tick = i
                };
                _pulsePacketHandler.Execute(pulsePacket, _session);
            }

            Assert.IsTrue(_session.LastPulse == pulsePacket.Tick);
        }
    }
}

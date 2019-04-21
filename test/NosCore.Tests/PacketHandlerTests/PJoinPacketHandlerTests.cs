using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Groups;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Groups;
using ChickenAPI.Packets.ServerPackets.Login;
using ChickenAPI.Packets.ServerPackets.Shop;
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
using NosCore.Data.Enumerations.Group;
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
using NosCore.GameObject.Networking.Group;
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
using NosCore.PacketHandlers.Group;
using NosCore.PacketHandlers.Login;
using NosCore.PacketHandlers.Shops;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PJoinPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly Dictionary<int, Character> _characters = new Dictionary<int, Character>();
        private PjoinPacketHandler _pJoinPacketHandler;

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            GroupAccess.Instance.Groups = new ConcurrentDictionary<long, Group>();
            for (byte i = 0; i < (byte)(GroupType.Group + 1); i++)
            {
                var session = new ClientSession(null, _logger, null) { SessionId = i };

                Broadcaster.Instance.RegisterSession(session);
                var acc = new AccountDto { Name = $"AccountTest{i}", Password = "test".ToSha512() };
                var charaDto = new Character(null, null, null, null, null, null, null, _logger, null)
                {
                    CharacterId = i,
                    Name = $"TestExistingCharacter{i}",
                    Slot = 1,
                    AccountId = acc.AccountId,
                    MapId = 1,
                    State = CharacterState.Active
                };

                session.InitializeAccount(acc);

                var chara = charaDto;
                chara.Session = session;
                chara.Account = acc;
                _characters.Add(i, chara);
                chara.Group.JoinGroup(chara);
                session.SetCharacter(chara);
                session.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true,
                    MapInstanceType.BaseMapInstance,
                     new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    null, _logger);
            }

            _pJoinPacketHandler = new PjoinPacketHandler(_logger);
        }

        [TestMethod]
        public void Test_Accept_Group_Join_Requested()
        {
            _characters[1].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue(_characters[0].Group.Count > 1
                && _characters[1].Group.Count > 1
                && _characters[0].Group.GroupId
                == _characters[1].Group.GroupId);
        }

        [TestMethod]
        public void Test_Join_Full_Group()
        {
            PjoinPacket pjoinPacket;

            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

                pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _characters[i].CharacterId
                };

                _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull);

            _characters[3].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[3].CharacterId
            };

            _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue(_characters[3].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Accept_Not_Requested_Group()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue(_characters[0].Group.Count == 1
                && _characters[1].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Decline_Not_Requested_Group()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = _characters[1].CharacterId
            };

            _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue(_characters[0].Group.Count == 1
                && _characters[1].Group.Count == 1);
        }
    }
}

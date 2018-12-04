using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExchangeInfo;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class ExchangePacketControllerTests
    {
        private ClientSession _session;
        private ClientSession _session2;
        private Character _character;
        private Character _character2;
        private ExchangePacketController _controller;
        private ExchangePacketController _controller2;
        private IItemBuilderService _itemBuilderService;

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            Broadcaster.Reset();

            var account = new AccountDto { Name = "AccountTest", Password = EncryptionHelper.Sha512("test") };
            var account2 = new AccountDto { Name = "AccountTest2", Password = EncryptionHelper.Sha512("test") };

            _character = new Character
            {
                CharacterId = 1,
                Name = "Test",
                Slot = 1,
                AccountId = 1,
                MapId = 1,
                State = CharacterState.Active,
                ExchangeInfo = new ExchangeInfoService()
            };

            _character2 = new Character
            {
                CharacterId = 2,
                Name = "Test2",
                Slot = 1,
                AccountId = 2,
                MapId = 1,
                State = CharacterState.Active,
                ExchangeInfo = new ExchangeInfoService()
            };
            var channelMock = new Mock<IChannel>();

            _session = new ClientSession(null, new List<PacketController> { new ExchangePacketController(_itemBuilderService) });
            _session.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;

            _controller = new ExchangePacketController(_itemBuilderService);
            _controller.RegisterSession(_session);
            _session.SetCharacter(_character);
            Broadcaster.Instance.RegisterSession(_session);

            _session2 = new ClientSession(null, new List<PacketController> { new ExchangePacketController(_itemBuilderService) });
            _session2.RegisterChannel(channelMock.Object);
            _session2.InitializeAccount(account2);
            _session2.SessionId = 2;

            _controller2 = new ExchangePacketController(_itemBuilderService);
            _controller2.RegisterSession(_session2);
            _session2.SetCharacter(_character2);
            Broadcaster.Instance.RegisterSession(_session2);
        }

        [TestMethod]
        public void Test_Accept_Request()
        {
            var packet = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.List,
                VisualId = _character2.CharacterId
            };

            _controller.RequestExchange(packet);
            Assert.IsTrue(_character.ExchangeInfo.ExchangeData.TargetVisualId == _character2.CharacterId && _character2.InExchangeOrShop);
        }

        [TestMethod]
        public void Test_Cancel_Request()
        {
            _character.ExchangeInfo.ExchangeData.TargetVisualId = _character2.VisualId;
            _character.InExchange = true;
            _character2.InExchange = true;

            var packet = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.Cancelled,
                VisualId = _character2.VisualId
            };

            _controller.RequestExchange(packet);
            Assert.IsTrue(!_character.InExchangeOrShop && !_character2.InExchangeOrShop);


        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
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
        private readonly List<ExchangePacketController> _controllers = new List<ExchangePacketController>();
        private readonly Dictionary<int, Character> _characters = new Dictionary<int, Character>();

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            Broadcaster.Reset();
            for (byte i = 0; i < 2; i++)
            {
                var handler = new ExchangePacketController();
                var session = new ClientSession(null, new List<PacketController> { handler }, null) { SessionId = i };


                Broadcaster.Instance.RegisterSession(session);
                var account = new AccountDto { Name = $"AccountTest{i}", Password = EncryptionHelper.Sha512("test") };

                var characterDto = new CharacterDto
                {
                    CharacterId = i,
                    Name = $"TestExistingCharacter{i}",
                    Slot = 1,
                    AccountId = account.AccountId,
                    MapId = 1,
                    State = CharacterState.Active
                };

                session.InitializeAccount(account);
                _controllers.Add(handler);
                handler.RegisterSession(session);

                var character = characterDto.Adapt<Character>();
                character.Session = session;
                _characters.Add(i, character);
                session.SetCharacter(character);
                session.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true,
                    MapInstanceType.BaseMapInstance, null);
            }
        }

        [TestMethod]
        public void Test_Accept_Request()
        {
            var packet = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.List,
                VisualId = _characters.ElementAt(1).Value.VisualId
            };

            _controllers.ElementAt(0).RequestExchange(packet);
            Assert.IsTrue(_characters.ElementAt(0).Value.ExchangeData.TargetVisualId == _characters.ElementAt(1).Value.CharacterId && _characters.ElementAt(1).Value.InExchangeOrTrade);
        }

        [TestMethod]
        public void Test_Cancel_Request()
        {
            _characters.ElementAt(0).Value.ExchangeData.TargetVisualId = _characters.ElementAt(1).Value.VisualId;
            _characters.ElementAt(0).Value.InExchangeOrTrade = true;
            _characters.ElementAt(1).Value.InExchangeOrTrade = true;

            var packet = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.Cancelled,
                VisualId = _characters.ElementAt(1).Value.VisualId
            };

            _controllers.ElementAt(0).RequestExchange(packet);
            Assert.IsTrue(_characters.ElementAt(0).Value.InExchangeOrTrade == false && _characters.ElementAt(1).Value.InExchangeOrTrade == false);


        }
    }
}

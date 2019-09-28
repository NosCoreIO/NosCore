using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharacterDeletePacketHandlerTests
    {
        private CharacterDeletePacketHandler _characterDeletePacketHandler;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            new Mapper();
            _session = TestHelpers.Instance.GenerateSession();
            _characterDeletePacketHandler =
                new CharacterDeletePacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao);
        }

        [TestMethod]
        public void DeleteCharacter_Invalid_Password()
        {
            _session.Character.MapInstance = null;
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "testpassword"
            }, _session);
            Assert.IsNotNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s =>
                        (s.AccountId == _session.Account.AccountId) && (s.State == CharacterState.Active)));
        }

        [TestMethod]
        public void DeleteCharacterWhenInGame_Does_Not_Delete_Character()
        {
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session);
            Assert.IsNotNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s =>
                        (s.AccountId == _session.Account.AccountId) && (s.State == CharacterState.Active)));
        }

        [TestMethod]
        public void DeleteCharacter()
        {
            _session.Character.MapInstance = null;
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session);
            Assert.IsNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s =>
                        (s.AccountId == _session.Account.AccountId) && (s.State == CharacterState.Active)));
        }
    }
}